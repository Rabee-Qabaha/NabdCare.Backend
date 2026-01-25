using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserRepository
{
    // ============================================
    // ACTIVE USER QUERIES
    // ============================================

    /// <summary>
    /// Get active user by ID with related entities (Clinic, Role).
    /// Returns null if user is deleted or doesn't exist.
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Get active user by email address.
    /// Returns null if user is deleted or doesn't exist.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Get paginated active users for all clinics (SuperAdmin only).
    /// </summary>
    Task<PaginatedResult<User>> GetAllPagedAsync(
        UserFilterRequestDto filter,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null);

    /// <summary>
    /// Get paginated active users for a specific clinic.
    /// </summary>
    Task<PaginatedResult<User>> GetByClinicIdPagedAsync(
        Guid clinicId,
        int limit,
        string? cursor,
        bool includeDeleted = false,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null);

    /// <summary>
    /// Get all active users assigned to a specific role.
    /// </summary>
    Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId);

    /// <summary>
    /// Check if active email already exists (case-insensitive).
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Check if active user exists by ID.
    /// </summary>
    Task<bool> ExistsAsync(Guid userId);

    // ============================================
    // SOFT-DELETE BYPASS QUERIES (Security Sensitive)
    // ============================================

    /// <summary>
    /// Get user by ID INCLUDING soft-deleted users.
    /// ⚠️ SECURITY CRITICAL: Use ONLY for authorization checks or preventing login of deleted users.
    /// Returns minimal data set (Clinic, Role).
    /// </summary>
    Task<User?> GetByIdForAuthorizationAsync(Guid userId);

    /// <summary>
    /// Get user by ID INCLUDING soft-deleted users.
    /// ⚠️ SECURITY CRITICAL: Use ONLY for user restoration processes.
    /// Returns complete data set including audit trails.
    /// </summary>
    Task<User?> GetByIdForRestorationAsync(Guid userId);

    /// <summary>
    /// Get user by email INCLUDING soft-deleted users.
    /// ⚠️ SECURITY CRITICAL: Use ONLY for email uniqueness validation.
    /// </summary>
    Task<User?> GetByEmailIncludingDeletedAsync(string email);

    /// <summary>
    /// Check if email exists in any user record (active or deleted).
    /// </summary>
    Task<bool> EmailExistsIncludingDeletedAsync(string email);

    /// <summary>
    /// Check if user record exists (active or deleted).
    /// </summary>
    Task<bool> UserExistsIncludingDeletedAsync(Guid userId);

    // ============================================
    // COMMAND METHODS
    // ============================================

    /// <summary>
    /// Create a new user.
    /// </summary>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Update existing user.
    /// </summary>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Efficiently updates only the LastLoginAt timestamp without modifying other fields.
    /// </summary>
    Task UpdateLastLoginAsync(Guid userId);
    
    /// <summary>
    /// Soft delete user (marks as deleted, can be restored).
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid userId);

    /// <summary>
    /// Permanently delete user (irreversible).
    /// </summary>
    Task<bool> DeleteAsync(Guid userId);
    
    /// <summary>
    /// Count active users for a specific clinic.
    /// </summary>
    Task<int> CountByClinicIdAsync(Guid clinicId);
}