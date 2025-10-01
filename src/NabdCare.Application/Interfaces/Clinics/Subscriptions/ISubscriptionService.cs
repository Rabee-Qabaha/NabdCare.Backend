using NabdCare.Application.DTOs.Clinics.Subscriptions;

namespace NabdCare.Application.Interfaces.Clinics.Subscriptions;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateSubscriptionAsync(CreateSubscriptionRequestDto dto);
    Task<SubscriptionResponseDto?> GetByIdAsync(Guid id, bool includePayments = false);
    Task<IEnumerable<SubscriptionResponseDto>> GetByClinicIdAsync(Guid clinicId, bool includePayments = false);
    Task<IEnumerable<SubscriptionResponseDto>> GetPagedAsync(int page, int pageSize, bool includePayments = false);
    Task<SubscriptionResponseDto?> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionRequestDto dto);
    Task<bool> SoftDeleteSubscriptionAsync(Guid id);
    Task<bool> DeleteSubscriptionAsync(Guid id);
}