// NabdCare.Application/Interfaces/Clinics/Subscriptions/ISubscriptionRepository.cs
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Interfaces.Clinics.Subscriptions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false);

    Task<PaginatedResult<Subscription>> GetByClinicIdPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null);

    Task<PaginatedResult<Subscription>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null);

    Task<PaginatedResult<Subscription>> GetPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null);

    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);
    Task<Subscription> UpdateStatusAsync(Subscription subscription);

    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Subscriptions that ended on/before 'now' but are still within their grace window,
    /// and have AutoRenew enabled.
    /// </summary>
    Task<List<Subscription>> GetAutoRenewCandidatesAsync(DateTime nowUtc);
}