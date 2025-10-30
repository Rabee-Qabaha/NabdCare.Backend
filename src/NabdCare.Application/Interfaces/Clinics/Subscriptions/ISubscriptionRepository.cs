using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Interfaces.Clinics.Subscriptions;

/// <summary>
/// Repository interface for managing clinic subscriptions.
/// Handles direct data access and filtering logic.
/// </summary>
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false);

    /// <summary>
    /// Get subscriptions for a specific clinic (paginated)
    /// </summary>
    Task<PaginatedResult<Subscription>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto pagination, bool includePayments = false);

    /// <summary>
    /// Get all subscriptions (paginated)
    /// </summary>
    Task<PaginatedResult<Subscription>> GetAllPagedAsync(PaginationRequestDto pagination, bool includePayments = false);

    /// <summary>
    /// Get paged subscriptions with optional filtering (SuperAdmin only)
    /// </summary>
    Task<PaginatedResult<Subscription>> GetPagedAsync(PaginationRequestDto pagination, bool includePayments = false);

    Task<Subscription> CreateAsync(Subscription subscription);
    Task<Subscription> UpdateAsync(Subscription subscription);

    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}