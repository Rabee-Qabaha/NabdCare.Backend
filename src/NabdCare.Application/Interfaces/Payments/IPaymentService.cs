using NabdCare.Application.DTOs.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Payments;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequestDto request);
    Task<PaymentDto> GetPaymentByIdAsync(Guid id);
    Task<IEnumerable<PaymentDto>> GetPaymentsByClinicAsync(Guid clinicId);
    Task<IEnumerable<PaymentDto>> GetPaymentsByPatientAsync(Guid patientId);
    
    // Allocation Logic
    Task AllocatePaymentToInvoiceAsync(Guid paymentId, Guid invoiceId, decimal amount);
    
    // ✅ Correction Logic
    Task DeallocatePaymentAsync(Guid paymentId, Guid invoiceId); // Unlink money (Return to Credit)
    Task CancelPaymentAsync(Guid paymentId, string reason); // Void payment (Entry Error)

    // Refund & Status Management
    Task RefundPaymentAsync(Guid paymentId, string reason, decimal? amount = null);
    Task UpdateChequeStatusAsync(Guid paymentId, ChequeStatus newStatus);
}