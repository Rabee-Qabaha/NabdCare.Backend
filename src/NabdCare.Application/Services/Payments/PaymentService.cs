using AutoMapper;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
using NabdCare.Application.DTOs.Payments;
using NabdCare.Application.Interfaces.Invoices;
using NabdCare.Application.Interfaces.Payments;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;

namespace NabdCare.Application.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IMapper _mapper;
    private readonly IAccessPolicy<Payment> _policy;
    private readonly ITenantContext _tenantContext;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        IMapper mapper,
        IAccessPolicy<Payment> policy,
        ITenantContext tenantContext)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _mapper = mapper;
        _policy = policy;
        _tenantContext = tenantContext;
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequestDto request)
    {
        var payment = _mapper.Map<Payment>(request);
        
        // Ensure ClinicId is set from context if not provided (or override it for security)
        if (_tenantContext.ClinicId.HasValue)
        {
            payment.ClinicId = _tenantContext.ClinicId.Value;
        }
        
        if (request.Method == PaymentMethod.Cheque && request.ChequeDetail != null)
        {
            payment.ChequeDetail = _mapper.Map<ChequePaymentDetail>(request.ChequeDetail);
            payment.Status = PaymentStatus.Pending;
        }
        else
        {
            payment.Status = PaymentStatus.Completed;
        }

        var createdPayment = await _paymentRepository.CreateAsync(payment);

        if (request.Allocations != null && request.Allocations.Any())
        {
            foreach (var allocationReq in request.Allocations)
            {
                await AllocatePaymentToInvoiceAsync(createdPayment.Id, allocationReq.InvoiceId, allocationReq.Amount);
            }
        }

        var result = await _paymentRepository.GetByIdAsync(createdPayment.Id, includeChequeDetails: true);
        return _mapper.Map<PaymentDto>(result);
    }

    public async Task AllocatePaymentToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "write", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");

        // Note: Invoice policy check could be added here too, but usually payment ownership implies invoice access in same clinic.
        if (invoice.ClinicId != payment.ClinicId)
             throw new DomainException(ErrorCodes.INVALID_OPERATION, "Payment and Invoice must belong to the same clinic.");

        if (amount > payment.UnallocatedAmount)
        {
            throw new DomainException(ErrorCodes.INSUFFICIENT_FUNDS, 
                $"Insufficient funds in payment. Available: {payment.UnallocatedAmount}, Requested: {amount}");
        }

        if (amount > invoice.BalanceDue)
        {
            throw new DomainException(ErrorCodes.OVERPAYMENT, 
                $"Amount exceeds invoice balance. Due: {invoice.BalanceDue}, Requested: {amount}");
        }

        var allocation = new PaymentAllocation
        {
            PaymentId = paymentId,
            InvoiceId = invoiceId,
            Amount = amount,
            AllocationDate = DateTime.UtcNow
        };

        payment.Allocations.Add(allocation);
        
        invoice.PaidAmount += amount;
        
        if (invoice.BalanceDue <= 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _paymentRepository.UpdateAsync(payment);
        await _invoiceRepository.UpdateAsync(invoice);
    }

    public async Task DeallocatePaymentAsync(Guid paymentId, Guid invoiceId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "write", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        var allocation = payment.Allocations.FirstOrDefault(a => a.InvoiceId == invoiceId);
        if (allocation == null) throw new KeyNotFoundException("Allocation not found for this invoice.");

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        if (invoice != null)
        {
            invoice.PaidAmount -= allocation.Amount;
            
            if (invoice.PaidAmount <= 0)
            {
                invoice.Status = InvoiceStatus.Sent;
                invoice.PaidDate = null;
            }
            else
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }
            
            await _invoiceRepository.UpdateAsync(invoice);
        }

        payment.Allocations.Remove(allocation);
        await _paymentRepository.UpdateAsync(payment);
    }

    public async Task CancelPaymentAsync(Guid paymentId, string reason)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, includeChequeDetails: true);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "delete", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        if (payment.Status == PaymentStatus.Failed || payment.Status == PaymentStatus.Refunded)
            throw new DomainException(ErrorCodes.INVALID_OPERATION, "Payment is already cancelled or refunded.");

        await ReverseAllocationsAsync(payment);

        payment.Status = PaymentStatus.Failed;
        payment.Notes = string.IsNullOrEmpty(payment.Notes) 
            ? $"Cancelled: {reason}" 
            : $"{payment.Notes} | Cancelled: {reason}";

        await _paymentRepository.UpdateAsync(payment);
    }

    public async Task RefundPaymentAsync(Guid paymentId, string reason, decimal? amount = null)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, includeChequeDetails: true);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "write", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        if (payment.Status == PaymentStatus.Refunded || payment.Status == PaymentStatus.Failed)
        {
            throw new DomainException(ErrorCodes.INVALID_OPERATION, "Payment is already fully refunded or failed.");
        }

        decimal refundAmount = amount ?? (payment.Amount - payment.RefundedAmount);
        
        if (refundAmount <= 0) throw new DomainException(ErrorCodes.INVALID_REFUND_AMOUNT, "Refund amount must be positive.");
        if (refundAmount > (payment.Amount - payment.RefundedAmount)) 
            throw new DomainException(ErrorCodes.INVALID_REFUND_AMOUNT, "Cannot refund more than the remaining payment balance.");

        decimal unallocated = payment.UnallocatedAmount;
        decimal toRefundFromUnallocated = Math.Min(refundAmount, unallocated);
        decimal remainingRefund = refundAmount - toRefundFromUnallocated;

        if (remainingRefund > 0)
        {
            foreach (var allocation in payment.Allocations.OrderByDescending(a => a.AllocationDate).ToList())
            {
                if (remainingRefund <= 0) break;

                decimal amountToReverse = Math.Min(allocation.Amount, remainingRefund);
                
                var invoice = await _invoiceRepository.GetByIdAsync(allocation.InvoiceId);
                if (invoice != null)
                {
                    invoice.PaidAmount -= amountToReverse;
                    if (invoice.PaidAmount < invoice.TotalAmount)
                    {
                        invoice.Status = invoice.PaidAmount <= 0 ? InvoiceStatus.Sent : InvoiceStatus.PartiallyPaid;
                        if (invoice.PaidAmount <= 0) invoice.PaidDate = null;
                    }
                    await _invoiceRepository.UpdateAsync(invoice);
                }

                if (amountToReverse == allocation.Amount)
                {
                    payment.Allocations.Remove(allocation);
                }
                else
                {
                    allocation.Amount -= amountToReverse;
                }

                remainingRefund -= amountToReverse;
            }
        }

        payment.RefundedAmount += refundAmount;
        payment.Notes = string.IsNullOrEmpty(payment.Notes) 
            ? $"Refunded: {refundAmount} ({reason})" 
            : $"{payment.Notes} | Refunded: {refundAmount} ({reason})";

        if (payment.RefundedAmount >= payment.Amount)
        {
            payment.Status = PaymentStatus.Refunded;
        }

        await _paymentRepository.UpdateAsync(payment);
    }

    public async Task UpdateChequeStatusAsync(Guid paymentId, ChequeStatus newStatus)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, includeChequeDetails: true);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "write", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        if (payment.ChequeDetail == null)
        {
            throw new DomainException(ErrorCodes.INVALID_OPERATION, "This payment is not a cheque payment.");
        }

        payment.ChequeDetail.Status = newStatus;

        switch (newStatus)
        {
            case ChequeStatus.Cleared:
                payment.Status = PaymentStatus.Completed;
                payment.ChequeDetail.ClearedDate = DateTime.UtcNow;
                break;

            case ChequeStatus.Bounced:
            case ChequeStatus.Cancelled:
                payment.Status = PaymentStatus.Failed;
                await ReverseAllocationsAsync(payment);
                break;

            case ChequeStatus.Pending:
                payment.Status = PaymentStatus.Pending;
                break;
        }

        await _paymentRepository.UpdateAsync(payment);
    }

    private async Task ReverseAllocationsAsync(Payment payment)
    {
        foreach (var allocation in payment.Allocations.ToList())
        {
            var invoice = await _invoiceRepository.GetByIdAsync(allocation.InvoiceId);
            if (invoice != null)
            {
                invoice.PaidAmount -= allocation.Amount;

                if (invoice.PaidAmount <= 0)
                {
                    invoice.Status = InvoiceStatus.Sent; 
                    invoice.PaidDate = null;
                }
                else
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                await _invoiceRepository.UpdateAsync(invoice);
            }
        }
        
        payment.Allocations.Clear();
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, includeChequeDetails: true);
        if (payment == null) return null; // Or throw KeyNotFound, but null is standard for GetById

        if (!await _policy.EvaluateAsync(_tenantContext, "read", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByClinicAsync(Guid clinicId)
    {
        // Policy check for list access is usually done via filtering (ABAC)
        // Here we assume the caller (Endpoint) has validated that the user can access this clinic's data
        // OR we enforce it here:
        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
             throw new UnauthorizedAccessException("Access denied to this clinic's payments.");

        var payments = await _paymentRepository.GetByClinicIdAsync(clinicId, includeChequeDetails: true);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByPatientAsync(Guid patientId)
    {
        // Similar check for patient ownership if needed
        var payments = await _paymentRepository.GetByPatientIdAsync(patientId, includeChequeDetails: true);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }
}