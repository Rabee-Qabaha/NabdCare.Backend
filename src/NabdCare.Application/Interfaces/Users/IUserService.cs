using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserService
{
    // ========= QUERY =========
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);

    /// <summary>SuperAdmin: all users (paged).</summary>
    Task<PaginatedResult<UserResponseDto>> GetAllPagedAsync(int limit, string? cursor, bool includeDeleted = false);

    /// <summary>Clinic users (paged). SuperAdmin can pass any clinicId. ClinicAdmin is restricted to their clinic.</summary>
    Task<PaginatedResult<UserResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, int limit, string? cursor, bool includeDeleted = false);

    Task<UserResponseDto?> GetCurrentUserAsync();

    /// <summary>
    /// Check if an email exists and whether the user is soft-deleted
    /// </summary>
    Task<(bool exists, bool isDeleted, Guid? userId)> EmailExistsDetailedAsync(string email);

    /// <summary>
    /// Restore soft deleted user by email (case-insensitive)
    /// </summary>
    Task<UserResponseDto?> RestoreUserAsync(Guid id);
    
    // ========= COMMANDS =========
    Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto);
    Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto);
    Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, Guid roleId);
    Task<UserResponseDto?> ActivateUserAsync(Guid id);
    Task<UserResponseDto?> DeactivateUserAsync(Guid id);
    Task<bool> SoftDeleteUserAsync(Guid id);
    Task<bool> HardDeleteUserAsync(Guid id);

    // ========= PASSWORD =========
    Task<UserResponseDto> ChangePasswordAsync(Guid id, ChangePasswordRequestDto dto);
    Task<UserResponseDto> ResetPasswordAsync(Guid id, ResetPasswordRequestDto dto);
}