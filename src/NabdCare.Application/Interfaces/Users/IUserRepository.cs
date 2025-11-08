using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces.Users;

/// <summary>
/// Repository interface for user data access operations.
/// Provides comprehensive CRUD operations with multi-tenant support.
/// 
/// DESIGN PHILOSOPHY:
/// - Public methods are purpose-specific (SOLID - Interface Segregation)
/// - Each method has ONE clear responsibility (SOLID - Single Responsibility)
/// - Method names explicitly state purpose (self-documenting code)
/// </summary>
public interface IUserRepository
{
    // ============================================
    // ACTIVE USER QUERIES (Soft-Delete Filtered)
    // Use these for normal business operations
    // ============================================

    /// <summary>
    /// Get active user by ID with related entities (Clinic, Role)
    /// Returns null if user is deleted or doesn't exist
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Get active user by email address
    /// Returns null if user is deleted or doesn't exist
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Get paginated active users for all clinics (SuperAdmin only).
    /// Cursor-based pagination for high scalability.
    /// </summary>
    Task<PaginatedResult<User>> GetAllPagedAsync(
        int limit,
        string? cursor,
        bool includeDeleted = false,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null);

    /// <summary>
    /// Get paginated active users for a specific clinic.
    /// Used by ClinicAdmin or system features limited to one clinic.
    /// </summary>
    Task<PaginatedResult<User>> GetByClinicIdPagedAsync(
        Guid clinicId,
        int limit,
        string? cursor,
        bool includeDeleted = false,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null);

    /// <summary>
    /// Get all active users assigned to a specific role
    /// </summary>
    Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId);

    /// <summary>
    /// Check if active email already exists (case-insensitive)
    /// Returns false for deleted users' emails
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Check if active user exists by ID
    /// Returns false for deleted users
    /// </summary>
    Task<bool> ExistsAsync(Guid userId);

    // ============================================
    // SOFT-DELETE BYPASS QUERIES
    // Purpose-specific methods (SOLID-compliant)
    // Each method has explicit, single responsibility
    // ============================================

    /// <summary>
    /// Get user by ID INCLUDING soft-deleted users - FOR AUTHORIZATION CHECKS.
    /// 
    /// PURPOSE: 
    /// - Authentication: Determine if deleted user should be allowed to login
    /// - Permission Evaluation: Check role/clinic of deleted user
    /// - Authorization: Validate permissions even for deleted accounts
    /// 
    /// USE CASES:
    /// - PermissionEvaluator: Check if user (even if deleted) has permission
    /// - AuthenticationService: Prevent deleted users from logging in
    /// 
    /// DATA RETURNED: 
    /// - Minimal set (Clinic, Role only) - optimized for auth performance
    /// - No audit trail data
    /// 
    /// ⚠️ SECURITY CRITICAL: 
    /// - Bypasses soft-delete filter
    /// - Logged for audit trail
    /// - Call ONLY from PermissionEvaluator or AuthenticationService
    /// - NEVER call from general user lookup endpoints
    /// 
    /// STACK TRACE: Logged for security investigation
    /// </summary>
    Task<User?> GetByIdForAuthorizationAsync(Guid userId);

    /// <summary>
    /// Get user by ID INCLUDING soft-deleted users - FOR USER RESTORATION.
    /// 
    /// PURPOSE: 
    /// - User Restoration: Retrieve deleted user to restore account
    /// - Admin Recovery: Allow admins to recover accidentally deleted users
    /// 
    /// USE CASES:
    /// - UserService.RestoreUserAsync: Restore a deleted user account
    /// - Admin Portal: Show deleted users for recovery
    /// 
    /// DATA RETURNED: 
    /// - Complete set (Clinic, Role, CreatedByUser) - needed for full restoration
    /// - Includes audit trail information
    /// 
    /// ⚠️ SECURITY CRITICAL: 
    /// - Bypasses soft-delete filter
    /// - Logged for audit trail
    /// - Call ONLY from UserService.RestoreUserAsync
    /// - Authorization checks MUST be performed in calling service
    /// 
    /// STACK TRACE: Logged for security investigation
    /// </summary>
    Task<User?> GetByIdForRestorationAsync(Guid userId);

    /// <summary>
    /// Get user by email INCLUDING soft-deleted users - FOR EMAIL VERIFICATION.
    /// 
    /// PURPOSE:
    /// - Email Duplicate Detection: Detect if email was previously used
    /// - Signup Validation: Prevent email reuse across deleted accounts
    /// - Email Change: Validate new email isn't taken by deleted user
    /// 
    /// USE CASES:
    /// - UserService.EmailExistsDetailedAsync: Check email availability
    /// - Signup Validation: Prevent duplicate emails
    /// 
    /// DATA RETURNED:
    /// - User object if found (including soft-deleted)
    /// - Null if email never used
    /// 
    /// ⚠️ SECURITY CRITICAL:
    /// - Bypasses soft-delete filter
    /// - Logged for audit trail
    /// - Call ONLY from UserService email validation methods
    /// 
    /// STACK TRACE: Logged for security investigation
    /// </summary>
    Task<User?> GetByEmailIncludingDeletedAsync(string email);

    /// <summary>
    /// Check if email exists INCLUDING soft-deleted users.
    /// Returns true if email exists in any user (active or deleted).
    /// </summary>
    Task<bool> EmailExistsIncludingDeletedAsync(string email);

    /// <summary>
    /// Check if user exists INCLUDING soft-deleted users.
    /// Returns true if user record exists (active or deleted).
    /// </summary>
    Task<bool> UserExistsIncludingDeletedAsync(Guid userId);

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