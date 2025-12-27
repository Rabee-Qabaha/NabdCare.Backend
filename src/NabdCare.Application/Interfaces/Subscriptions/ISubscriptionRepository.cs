using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Subscriptions;

namespace NabdCare.Application.Interfaces.Subscriptions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false, bool includeInvoices = false);
    
    // Lifecycle Queries
    Task<Subscription?> GetActiveByClinicIdAsync(Guid clinicId);
    Task<bool> HasFutureSubscriptionAsync(Guid clinicId, DateTime afterDate);
    
    // Batch Queries for Background Jobs
    Task<List<Subscription>> GetAutoRenewCandidatesAsync(DateTime nowUtc);
    Task<List<Subscription>> GetCancellationCandidatesAsync(DateTime nowUtc);
    Task<List<Subscription>> GetExpiredCandidatesAsync(DateTime nowUtc);
    Task<List<Subscription>> GetFutureSubscriptionsStartingByAsync(DateTime date);
    Task<List<Subscription>> GetFutureSubscriptionsByClinicAsync(Guid clinicId);

    // Cursor Pagination
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

    // Command Methods
    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);
    Task<bool> UpdateStatusAsync(Subscription subscription);
    Task<bool> DeleteAsync(Guid id);
}