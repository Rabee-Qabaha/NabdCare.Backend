using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITenantContext tenantContext,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _logger = logger;
    }

    #region User CRUD

    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto)
    {
        var user = _mapper.Map<User>(dto);

        // Assign clinic
        user.ClinicId = _tenantContext.IsSuperAdmin
            ? dto.ClinicId
            : _tenantContext.ClinicId;

        // Hash password
        user.PasswordHash = _passwordService.HashPassword(user, dto.Password);

        var created = await _userRepository.CreateUserAsync(user);
        _logger.LogInformation("User {UserId} created by {Actor}", created.Id, _tenantContext.ClinicId);

        return _mapper.Map<UserResponseDto>(created);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        return user == null ? null : _mapper.Map<UserResponseDto>(user);
    }

    public async Task<IEnumerable<UserResponseDto>> GetUsersByClinicIdAsync(Guid? clinicId)
    {
        var users = await _userRepository.GetUsersByClinicIdAsync(clinicId);
        return _mapper.Map<IEnumerable<UserResponseDto>>(users);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null) return null;

        _mapper.Map(dto, user);

        var updated = await _userRepository.UpdateUserAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, UserRole role)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null) return null;

        if (!_tenantContext.IsSuperAdmin && role == UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("ClinicAdmin cannot assign SuperAdmin role.");

        user.Role = role;
        var updated = await _userRepository.UpdateUserAsync(user);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        try
        {
            var deleted = await _userRepository.DeleteUserAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Delete failed: User {UserId} not found.", id);
                return false;
            }

            _logger.LogInformation("User {UserId} permanently deleted by {Actor}", id, _tenantContext.ClinicId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        try
        {
            var success = await _userRepository.SoftDeleteUserAsync(id);
            if (!success)
            {
                _logger.LogWarning("Soft delete failed: User {UserId} not found.", id);
                return false;
            }

            _logger.LogInformation("User {UserId} soft deleted by {Actor}", id, _tenantContext.ClinicId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting user {UserId}", id);
            throw;
        }
    }

    #endregion

    #region Password Management

    public async Task<UserResponseDto> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (!_passwordService.VerifyPassword(dto.CurrentPassword, dto.CurrentPassword))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        var updated = await _userRepository.UpdateUserAsync(user);
        _logger.LogInformation("User {UserId} changed their password.", userId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        var updated = await _userRepository.UpdateUserAsync(user);
        _logger.LogInformation("ClinicAdmin {Actor} reset password for user {UserId}.", _tenantContext.ClinicId, userId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> AdminResetPasswordAsync(Guid userId, ResetPasswordRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can perform this action.");

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        var updated = await _userRepository.UpdateUserAsync(user);
        _logger.LogInformation("SuperAdmin reset password for user {UserId}.", userId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    #endregion
}