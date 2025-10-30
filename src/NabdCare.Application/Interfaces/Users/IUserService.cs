﻿using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;

namespace NabdCare.Application.Interfaces.Users;

public interface IUserService
{
    // ========= QUERY =========
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);

    /// <summary>SuperAdmin: all users (paged).</summary>
    Task<PaginatedResult<UserResponseDto>> GetAllPagedAsync(int limit, string? cursor);

    /// <summary>Clinic users (paged). SuperAdmin can pass any clinicId. ClinicAdmin is restricted to their clinic.</summary>
    Task<PaginatedResult<UserResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, int limit, string? cursor);

    Task<UserResponseDto?> GetCurrentUserAsync();

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
    Task<UserResponseDto> AdminResetPasswordAsync(Guid id, ResetPasswordRequestDto dto);
}