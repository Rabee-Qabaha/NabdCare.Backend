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
    /// Get user by email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// Get all users (SuperAdmin only)
    /// </summary>
    Task<IEnumerable<User>> GetAllAsync();
    
    /// <summary>
    /// Get users by clinic ID. If null, returns all users.
    /// </summary>
    Task<IEnumerable<User>> GetByClinicIdAsync(Guid clinicId);
    
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