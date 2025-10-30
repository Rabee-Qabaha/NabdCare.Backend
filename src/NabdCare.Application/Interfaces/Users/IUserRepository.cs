using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces.Users;

/// <summary>
/// Repository interface for user data access operations.
/// Provides comprehensive CRUD operations with multi-tenant support.
/// </summary>
public interface IUserRepository
{
    // ============================================
    // QUERY METHODS
    // ============================================

    /// <summary>
    /// Get user by ID with related entities (Clinic, Role)
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Get user without tenant filters (security-only usage)
    /// </summary>
    Task<User?> GetByIdRawAsync(Guid id);

    /// <summary>
    /// Get user by email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Get paginated users for all clinics (SuperAdmin only).
    /// Cursor-based pagination for high scalability.
    /// </summary>
    Task<PaginatedResult<User>> GetAllPagedAsync(int limit, string? cursor);

    /// <summary>
    /// Get paginated users for a specific clinic.
    /// Used by ClinicAdmin or system features limited to one clinic.
    /// </summary>
    Task<PaginatedResult<User>> GetByClinicIdPagedAsync(Guid clinicId, int limit, string? cursor);

    /// <summary>
    /// Check if email already exists (case-insensitive)
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Check if user exists by ID
    /// </summary>
    Task<bool> ExistsAsync(Guid userId);

    // ============================================
    // COMMAND METHODS
    // ============================================

    /// <summary>
    /// Create a new user
    /// </summary>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Update existing user
    /// </summary>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Soft delete user (marks as deleted, can be restored)
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid userId);

    /// <summary>
    /// Permanently delete user (irreversible)
    /// </summary>
    Task<bool> DeleteAsync(Guid userId);
}