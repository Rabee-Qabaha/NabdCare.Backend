using AutoMapper;
using FluentValidation;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Payments;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Configuration;
using NabdCare.Application.Interfaces.Invoices;
using NabdCare.Application.Interfaces.Payments;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;

namespace NabdCare.Application.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreatePaymentRequestDto> _paymentValidator;
    private readonly IAccessPolicy<Payment> _policy;
    private readonly ITenantContext _tenantContext;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        IClinicRepository clinicRepository,
        IExchangeRateService exchangeRateService,
        IMapper mapper,
        IValidator<CreatePaymentRequestDto> paymentValidator,
        IAccessPolicy<Payment> policy,
        ITenantContext tenantContext)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _clinicRepository = clinicRepository;
        _exchangeRateService = exchangeRateService;
        _mapper = mapper;
        _paymentValidator = paymentValidator;
        _policy = policy;
        _tenantContext = tenantContext;
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequestDto request)
    {
        var validationResult = await _paymentValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var payment = _mapper.Map<Payment>(request);
        
        Guid clinicId;
        if (_tenantContext.ClinicId.HasValue)
        {
            clinicId = _tenantContext.ClinicId.Value;
        }
        else if (request.Context == PaymentContext.Clinic && request.ClinicId.HasValue)
        {
            clinicId = request.ClinicId.Value;
        }
        else if (request.Context == PaymentContext.Patient && request.PatientId.HasValue)
        {
            var patient = await _clinicRepository.GetPatientByIdAsync(request.PatientId.Value);
            if (patient == null) throw new KeyNotFoundException("Patient not found");
            clinicId = patient.ClinicId;
        }
        else
        {
            throw new DomainException("Could not determine the clinic for this payment.", ErrorCodes.INVALID_ARGUMENT);
        }

        payment.ClinicId = clinicId;

        var clinic = await _clinicRepository.GetByIdAsync(clinicId);
        if (clinic == null) throw new KeyNotFoundException("Clinic not found");

        var functionalCurrency = clinic.Settings.Currency;
        
        var (baseRate, finalRate) = await GetExchangeRatesAsync(clinic, request.Currency, functionalCurrency);
        payment.BaseExchangeRate = baseRate;
        payment.FinalExchangeRate = finalRate;
        payment.AmountInFunctionalCurrency = payment.Amount * finalRate;

        if (request.Method == PaymentMethod.Cheque && request.ChequeDetail != null)
        {
            var cheque = _mapper.Map<ChequePaymentDetail>(request.ChequeDetail);
            payment.ChequeDetail = cheque;
            payment.Status = PaymentStatus.Pending;

            var (chequeBaseRate, chequeFinalRate) = await GetExchangeRatesAsync(clinic, cheque.Currency, functionalCurrency);
            
            payment.AmountInFunctionalCurrency += cheque.Amount * chequeFinalRate;
        }
        else
        {
            payment.Status = PaymentStatus.Completed;
        }

        if (request.Context == PaymentContext.Patient)
        {
            if (!request.PatientId.HasValue)
            {
                throw new DomainException("PatientId is required when creating a patient payment.", ErrorCodes.INVALID_ARGUMENT, "PatientId");
            }
            payment.PatientId = request.PatientId.Value;
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

    public async Task<List<PaymentDto>> ProcessBatchPaymentAsync(BatchPaymentRequestDto request)
    {
        if (request.Payments == null || !request.Payments.Any())
            throw new DomainException("No payments provided in batch.", ErrorCodes.INVALID_ARGUMENT);
        
        using var transaction = await _paymentRepository.BeginTransactionAsync();
        var createdPayments = new List<PaymentDto>();

        try
        {
            foreach (var payReq in request.Payments)
            {
                var validationResult = await _paymentValidator.ValidateAsync(payReq);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                if (request.ClinicId.HasValue && !payReq.ClinicId.HasValue) payReq.ClinicId = request.ClinicId;
                if (request.PatientId.HasValue && !payReq.PatientId.HasValue) payReq.PatientId = request.PatientId;

                var paymentDto = await CreatePaymentAsync(payReq);
                createdPayments.Add(paymentDto);
            }

            if (request.InvoicesToPay != null && request.InvoicesToPay.Any())
            {
                var balances = createdPayments.ToDictionary(p => p.Id, p => p.Amount);
                
                foreach (var invReq in request.InvoicesToPay)
                {
                    decimal remainingToPay = invReq.Amount;
                    
                    foreach (var paymentId in balances.Keys.ToList())
                    {
                        if (remainingToPay <= 0) break;
                        
                        decimal available = balances[paymentId];
                        if (available <= 0) continue;

                        decimal amountToTake = Math.Min(remainingToPay, available);
                        
                        await AllocatePaymentToInvoiceAsync(paymentId, invReq.InvoiceId, amountToTake);
                        
                        balances[paymentId] -= amountToTake;
                        remainingToPay -= amountToTake;
                    }
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var refreshedPayments = new List<PaymentDto>();
        foreach (var p in createdPayments)
        {
            var fresh = await _paymentRepository.GetByIdAsync(p.Id, true);
            refreshedPayments.Add(_mapper.Map<PaymentDto>(fresh));
        }

        return refreshedPayments;
    }

    public async Task AllocatePaymentToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "write", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        var invoice = await _invoiceRepository.GetByIdForUpdateAsync(invoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");

        if (invoice.ClinicId != payment.ClinicId)
             throw new DomainException(ErrorCodes.INVALID_OPERATION, "Payment and Invoice must belong to the same clinic.");

        var allocatedSum = payment.Allocations?.Sum(a => a.Amount) ?? 0;
        var available = payment.Amount - payment.RefundedAmount - allocatedSum;

        if (amount > available)
        {
            throw new DomainException(ErrorCodes.INSUFFICIENT_FUNDS, 
                $"Insufficient funds in payment. Available: {available}, Requested: {amount}. (Total: {payment.Amount}, Allocated: {allocatedSum})");
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

        var invoice = await _invoiceRepository.GetByIdForUpdateAsync(invoiceId);
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
                
                var invoice = await _invoiceRepository.GetByIdForUpdateAsync(allocation.InvoiceId);
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

    public async Task UpdateChequeDetailsAsync(Guid paymentId, UpdateChequeDetailDto dto)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, includeChequeDetails: true);
        if (payment == null) throw new KeyNotFoundException("Payment not found");

        if (!await _policy.EvaluateAsync(_tenantContext, "write", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        if (payment.ChequeDetail == null)
        {
            throw new DomainException(ErrorCodes.INVALID_OPERATION, "This payment is not a cheque payment.");
        }

        if (payment.ChequeDetail.Status != ChequeStatus.Pending)
        {
            throw new DomainException(ErrorCodes.INVALID_OPERATION, "Cannot edit cheque details unless status is Pending.");
        }

        payment.ChequeDetail.ChequeNumber = dto.ChequeNumber;
        payment.ChequeDetail.BankName = dto.BankName;
        payment.ChequeDetail.Branch = dto.Branch;
        payment.ChequeDetail.IssueDate = dto.IssueDate;
        payment.ChequeDetail.DueDate = dto.DueDate;
        
        if (!string.IsNullOrEmpty(dto.ImageUrl))
        {
            payment.ChequeDetail.ImageUrl = dto.ImageUrl;
        }
        
        if (!string.IsNullOrEmpty(dto.Note))
        {
            payment.ChequeDetail.Note = dto.Note;
        }

        await _paymentRepository.UpdateAsync(payment);
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, includeChequeDetails: true);
        if (payment == null) return null; 

        if (!await _policy.EvaluateAsync(_tenantContext, "read", payment))
            throw new UnauthorizedAccessException("Access denied to this payment.");

        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaginatedResult<PaymentDto>> GetPaymentsByClinicPagedAsync(Guid clinicId, PaymentFilterRequestDto filter)
    {
        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
             throw new UnauthorizedAccessException("Access denied to this clinic's payments.");
        
        var result = await _paymentRepository.GetByClinicIdPagedAsync(clinicId, filter, includeChequeDetails: true);
        
        return new PaginatedResult<PaymentDto>
        {
            Items = _mapper.Map<IEnumerable<PaymentDto>>(result.Items),
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByPatientAsync(Guid patientId)
    {
        var payments = await _paymentRepository.GetByPatientIdAsync(patientId, includeChequeDetails: true);
        return _mapper.Map<IEnumerable<PaymentDto>>(payments);
    }

    private async Task ReverseAllocationsAsync(Payment payment)
    {
        foreach (var allocation in payment.Allocations.ToList())
        {
            var invoice = await _invoiceRepository.GetByIdForUpdateAsync(allocation.InvoiceId);
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

    private async Task<(decimal baseRate, decimal finalRate)> GetExchangeRatesAsync(Clinic clinic, Currency fromCurrency, Currency toCurrency)
    {
        if (fromCurrency == toCurrency)
        {
            return (1.0m, 1.0m);
        }

        var baseRate = await _exchangeRateService.GetRateAsync(toCurrency.ToString(), fromCurrency.ToString());
        
        var finalRate = baseRate;
        if (clinic.Settings.ExchangeRateMarkupType == MarkupType.Percentage && clinic.Settings.ExchangeRateMarkupValue > 0)
        {
            var markup = baseRate * (clinic.Settings.ExchangeRateMarkupValue / 100);
            finalRate += markup;
        }

        return (baseRate, finalRate);
    }
}