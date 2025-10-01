using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Interfaces.Clinics.Subscriptions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false);
    Task<IEnumerable<Subscription>> GetByClinicIdAsync(Guid clinicId, bool includePayments = false);
    Task<IEnumerable<Subscription>> GetAllAsync(bool includePayments = false);
    Task<IEnumerable<Subscription>> GetPagedAsync(int page, int pageSize, bool includePayments = false);

    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);

    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}