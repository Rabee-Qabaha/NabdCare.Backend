using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Payments;

namespace NabdCare.Application.Interfaces.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId, bool includeChequeDetails = false);
    Task<IEnumerable<Payment>> GetAllAsync(bool includeChequeDetails = false);
    
    Task<IEnumerable<Payment>> GetByClinicIdAsync(Guid clinicId, bool includeChequeDetails = false);
    
    // ✅ Paged Version
    Task<PaginatedResult<Payment>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto pagination, bool includeChequeDetails = false);

    Task<IEnumerable<Payment>> GetByPatientIdAsync(Guid patientId, bool includeChequeDetails = false);

    Task<IEnumerable<Payment>> GetPagedAsync(int page, int pageSize, bool includeChequeDetails = false);

    Task<Payment> CreateAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<bool> SoftDeleteAsync(Guid paymentId);
    Task<bool> DeleteAsync(Guid paymentId);
}