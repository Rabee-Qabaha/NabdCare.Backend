using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Clinics;

/// <summary>
/// Repository interface for clinic data access operations.
/// Thin data access layer - no business logic.
/// </summary>
public interface IClinicRepository
{
    // ============================================
    // QUERY METHODS
    // ============================================

    /// <summary>
    /// Get clinic by ID with subscriptions
    /// </summary>
    Task<Clinic?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all clinics (SuperAdmin only) with pagination, sorting, and filtering
    /// </summary>
    Task<PaginatedResult<Clinic>> GetAllPagedAsync(PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics by subscription status (Active, Pending, etc.)
    /// </summary>
    Task<PaginatedResult<Clinic>> GetByStatusPagedAsync(SubscriptionStatus status, PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics with active status and valid subscription (EndDate > Now)
    /// </summary>
    Task<PaginatedResult<Clinic>> GetActiveWithValidSubscriptionPagedAsync(PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics with expiring subscriptions (within days)
    /// </summary>
    Task<PaginatedResult<Clinic>> GetWithExpiringSubscriptionsPagedAsync(int withinDays, PaginationRequestDto pagination);

    /// <summary>
    /// Get clinics with expired subscriptions pagination
    /// </summary>
    Task<PaginatedResult<Clinic>> GetWithExpiredSubscriptionsPagedAsync(PaginationRequestDto pagination);
    
    /// <summary>
    /// Get clinics with expired subscriptions return all for the background job
    /// </summary>
    Task<IEnumerable<Clinic>> GetWithExpiredSubscriptionsAsync();

    /// <summary>
    /// Search clinics by name, email, or phone
    /// </summary>
    Task<PaginatedResult<Clinic>> SearchPagedAsync(string query, PaginationRequestDto pagination);

    /// <summary>
    /// Check if clinic name exists (excluding specified ID)
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Check if clinic email exists (excluding specified ID)
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null);

    /// <summary>
    /// Check if clinic exists by ID
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    // ============================================
    // COMMAND METHODS
    // ============================================

    Task<Clinic> CreateAsync(Clinic clinic);
    Task<Clinic> UpdateAsync(Clinic clinic);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);

    // ============================================
    // STATISTICS
    // ============================================

    Task<int> GetTotalCountAsync();
    Task<int> GetCountByStatusAsync(SubscriptionStatus status);
    Task<int> GetActiveCountAsync();
}