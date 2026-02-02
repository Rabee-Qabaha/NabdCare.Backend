using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Payments;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequestDto request);
    Task<PaymentDto> GetPaymentByIdAsync(Guid id);
    
    // ✅ Updated to support Pagination
    Task<PaginatedResult<PaymentDto>> GetPaymentsByClinicPagedAsync(Guid clinicId, PaginationRequestDto pagination);
    
    Task<IEnumerable<PaymentDto>> GetPaymentsByPatientAsync(Guid patientId);
    
    // Allocation Logic
    Task AllocatePaymentToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount);
    
    // Correction Logic
    Task DeallocatePaymentAsync(Guid paymentId, Guid invoiceId); 
    Task CancelPaymentAsync(Guid paymentId, string reason);

    // Refund & Status Management
    Task RefundPaymentAsync(Guid paymentId, string reason, decimal? amount = null);
    Task UpdateChequeStatusAsync(Guid paymentId, ChequeStatus newStatus);
}