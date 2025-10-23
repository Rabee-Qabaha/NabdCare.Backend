using NabdCare.Application.DTOs.Users;

namespace NabdCare.Application.Interfaces.Users;

/// <summary>
/// Service interface for user management operations.
/// All methods include multi-tenant security, audit logging, and comprehensive error handling.
/// </summary>
public interface IUserService
{
    // ============================================
    // QUERY METHODS
    // ============================================
    
    /// <summary>
    /// Get user by ID with multi-tenant filtering
    /// </summary>
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);
    
    /// <summary>
    /// Get all users. SuperAdmin: all users. ClinicAdmin: only clinic users.
    /// </summary>
    Task<IEnumerable<UserResponseDto>> GetUsersByClinicIdAsync(Guid? clinicId);
    
    /// <summary>
    /// Get current authenticated user's details
    /// </summary>
    Task<UserResponseDto?> GetCurrentUserAsync();

    // ============================================
    // COMMAND METHODS
    // ============================================
    
    /// <summary>
    /// Create a new user
    /// </summary>
    Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto);
    
    /// <summary>
    /// Update user information
    /// </summary>
    Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto);
    
    /// <summary>
    /// Update user's role
    /// </summary>
    Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, Guid roleId);
    
    /// <summary>
    /// Activate a deactivated user account
    /// </summary>
    Task<UserResponseDto?> ActivateUserAsync(Guid id);
    
    /// <summary>
    /// Deactivate a user account (prevents login)
    /// </summary>
    Task<UserResponseDto?> DeactivateUserAsync(Guid id);
    
    /// <summary>
    /// Soft delete user (can be restored)
    /// </summary>
    Task<bool> SoftDeleteUserAsync(Guid id);
    
    /// <summary>
    /// Permanently delete user (SuperAdmin only - IRREVERSIBLE)
    /// </summary>
    Task<bool> HardDeleteUserAsync(Guid id);

    // ============================================
    // PASSWORD MANAGEMENT
    // ============================================
    
    /// <summary>
    /// User changes their own password
    /// </summary>
    Task<UserResponseDto> ChangePasswordAsync(Guid id, ChangePasswordRequestDto dto);
    
    /// <summary>
    /// ClinicAdmin resets password for users in their clinic
    /// </summary>
    Task<UserResponseDto> ResetPasswordAsync(Guid id, ResetPasswordRequestDto dto);
    
    /// <summary>
    /// SuperAdmin resets password for any user in any clinic
    /// </summary>
    Task<UserResponseDto> AdminResetPasswordAsync(Guid id, ResetPasswordRequestDto dto);
}