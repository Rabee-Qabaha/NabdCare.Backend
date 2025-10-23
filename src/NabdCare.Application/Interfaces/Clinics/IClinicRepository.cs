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
    /// Get all clinics (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<Clinic>> GetAllAsync();
    
    /// <summary>
    /// Get clinics by subscription status
    /// </summary>
    Task<IEnumerable<Clinic>> GetByStatusAsync(SubscriptionStatus status);
    
    /// <summary>
    /// Get clinics with active status and valid subscription (EndDate > Now)
    /// </summary>
    Task<IEnumerable<Clinic>> GetActiveWithValidSubscriptionAsync();
    
    /// <summary>
    /// Get clinics with expiring subscriptions (within days)
    /// </summary>
    Task<IEnumerable<Clinic>> GetWithExpiringSubscriptionsAsync(int withinDays);
    
    /// <summary>
    /// Get clinics with expired subscriptions
    /// </summary>
    Task<IEnumerable<Clinic>> GetWithExpiredSubscriptionsAsync();
    
    /// <summary>
    /// Get paginated clinics
    /// </summary>
    Task<IEnumerable<Clinic>> GetPagedAsync(int page, int pageSize);
    
    /// <summary>
    /// Search clinics by name, email, or phone
    /// </summary>
    Task<IEnumerable<Clinic>> SearchAsync(string query);
    
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
    
    /// <summary>
    /// Create new clinic
    /// </summary>
    Task<Clinic> CreateAsync(Clinic clinic);
    
    /// <summary>
    /// Update existing clinic
    /// </summary>
    Task<Clinic> UpdateAsync(Clinic clinic);
    
    /// <summary>
    /// Soft delete clinic (can be restored)
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid id);
    
    /// <summary>
    /// Permanently delete clinic (irreversible)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    // ============================================
    // STATISTICS
    // ============================================
    
    /// <summary>
    /// Get total count of clinics
    /// </summary>
    Task<int> GetTotalCountAsync();
    
    /// <summary>
    /// Get count by subscription status
    /// </summary>
    Task<int> GetCountByStatusAsync(SubscriptionStatus status);
    
    /// <summary>
    /// Get count of clinics with active subscriptions
    /// </summary>
    Task<int> GetActiveCountAsync();
}