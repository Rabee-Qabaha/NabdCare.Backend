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
        // ✅ P0 FIX: Check email uniqueness
        var existingUser = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Attempt to create user with duplicate email: {Email}", dto.Email);
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists.");
        }

        var user = _mapper.Map<User>(dto);

        // ✅ Assign clinic based on context
        if (_tenantContext.IsSuperAdmin)
        {
            // SuperAdmin can specify ClinicId or leave null
            user.ClinicId = dto.ClinicId;
        }
        else
        {
            // ClinicAdmin can only create users in their clinic
            if (dto.ClinicId.HasValue && dto.ClinicId != _tenantContext.ClinicId)
            {
                _logger.LogWarning("ClinicAdmin {ActorId} attempted to create user in different clinic", _tenantContext.UserId);
                throw new UnauthorizedAccessException("You can only create users in your own clinic.");
            }
            user.ClinicId = _tenantContext.ClinicId;
        }

        // ✅ P0 FIX: Prevent non-SuperAdmin from creating SuperAdmin
        if (!_tenantContext.IsSuperAdmin && dto.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("ClinicAdmin {ActorId} attempted to create SuperAdmin user", _tenantContext.UserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can create SuperAdmin users.");
        }

        // ✅ Hash password
        user.PasswordHash = _passwordService.HashPassword(user, dto.Password);
        
        // ✅ Track creator
        user.CreatedByUserId = _tenantContext.UserId;

        var created = await _userRepository.CreateUserAsync(user);
        _logger.LogInformation("User {UserId} ({Email}) created by {ActorId} in clinic {ClinicId}", 
            created.Id, created.Email, _tenantContext.UserId, created.ClinicId);

        return _mapper.Map<UserResponseDto>(created);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return null;
        }

        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task<IEnumerable<UserResponseDto>> GetUsersByClinicIdAsync(Guid? clinicId)
    {
        // ✅ SuperAdmin can query specific clinic or all
        // ✅ ClinicAdmin can only query their own clinic (enforced by global filter)
        var users = await _userRepository.GetUsersByClinicIdAsync(clinicId);
        return _mapper.Map<IEnumerable<UserResponseDto>>(users);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Update failed: User {UserId} not found", id);
            return null;
        }

        // ✅ Prevent role escalation
        if (!_tenantContext.IsSuperAdmin && dto.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("ClinicAdmin {ActorId} attempted to set SuperAdmin role", _tenantContext.UserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can assign SuperAdmin role.");
        }

        // ✅ Prevent demotion of SuperAdmin by non-SuperAdmin
        if (!_tenantContext.IsSuperAdmin && user.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("ClinicAdmin {ActorId} attempted to modify SuperAdmin user {UserId}", 
                _tenantContext.UserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can modify SuperAdmin users.");
        }

        _mapper.Map(dto, user);

        var updated = await _userRepository.UpdateUserAsync(user);
        _logger.LogInformation("User {UserId} updated by {ActorId}", id, _tenantContext.UserId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, UserRole role)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Role update failed: User {UserId} not found", id);
            return null;
        }

        // ✅ Only SuperAdmin can assign SuperAdmin role
        if (!_tenantContext.IsSuperAdmin && role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("ClinicAdmin {ActorId} attempted to assign SuperAdmin role", _tenantContext.UserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can assign SuperAdmin role.");
        }

        // ✅ Only SuperAdmin can modify SuperAdmin users
        if (!_tenantContext.IsSuperAdmin && user.Role == UserRole.SuperAdmin)
        {
            _logger.LogWarning("ClinicAdmin {ActorId} attempted to change SuperAdmin {UserId} role", 
                _tenantContext.UserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can modify SuperAdmin users.");
        }

        user.Role = role;
        var updated = await _userRepository.UpdateUserAsync(user);

        _logger.LogInformation("User {UserId} role changed to {Role} by {ActorId}", 
            id, role, _tenantContext.UserId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Delete failed: User {UserId} not found", id);
                return false;
            }

            // ✅ Prevent deletion of SuperAdmin by non-SuperAdmin
            if (!_tenantContext.IsSuperAdmin && user.Role == UserRole.SuperAdmin)
            {
                _logger.LogWarning("ClinicAdmin {ActorId} attempted to delete SuperAdmin {UserId}", 
                    _tenantContext.UserId, id);
                throw new UnauthorizedAccessException("Only SuperAdmin can delete SuperAdmin users.");
            }

            // ✅ Prevent self-deletion
            if (id == _tenantContext.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete themselves", id);
                throw new InvalidOperationException("You cannot delete your own account.");
            }

            var deleted = await _userRepository.DeleteUserAsync(id);
            if (deleted)
            {
                _logger.LogInformation("User {UserId} permanently deleted by {ActorId}", 
                    id, _tenantContext.UserId);
            }

            return deleted;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Soft delete failed: User {UserId} not found", id);
                return false;
            }

            // ✅ Prevent soft deletion of SuperAdmin by non-SuperAdmin
            if (!_tenantContext.IsSuperAdmin && user.Role == UserRole.SuperAdmin)
            {
                _logger.LogWarning("ClinicAdmin {ActorId} attempted to soft delete SuperAdmin {UserId}", 
                    _tenantContext.UserId, id);
                throw new UnauthorizedAccessException("Only SuperAdmin can delete SuperAdmin users.");
            }

            // ✅ Prevent self-deletion
            if (id == _tenantContext.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to soft delete themselves", id);
                throw new InvalidOperationException("You cannot delete your own account.");
            }

            var success = await _userRepository.SoftDeleteUserAsync(id);
            if (success)
            {
                _logger.LogInformation("User {UserId} soft deleted by {ActorId}", 
                    id, _tenantContext.UserId);
            }

            return success;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not InvalidOperationException)
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
        {
            _logger.LogWarning("Change password failed: User {UserId} not found", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        // Verify current password correctly
        if (!_passwordService.VerifyPassword(user, dto.CurrentPassword))
        {
            _logger.LogWarning("Change password failed: Invalid current password for user {UserId}", userId);
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        // ✅ Hash new password
        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        var updated = await _userRepository.UpdateUserAsync(user);
        
        _logger.LogInformation("User {UserId} changed their password", userId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Reset password failed: User {UserId} not found", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        // ✅ Verify ClinicAdmin can only reset passwords in their clinic
        if (!_tenantContext.IsSuperAdmin && user.ClinicId != _tenantContext.ClinicId)
        {
            _logger.LogWarning("ClinicAdmin {ActorId} attempted to reset password for user {UserId} in different clinic", 
                _tenantContext.UserId, userId);
            throw new UnauthorizedAccessException("You can only reset passwords for users in your clinic.");
        }

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        var updated = await _userRepository.UpdateUserAsync(user);
        
        _logger.LogInformation("ClinicAdmin {ActorId} reset password for user {UserId}", 
            _tenantContext.UserId, userId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> AdminResetPasswordAsync(Guid userId, ResetPasswordRequestDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Admin reset password failed: User {UserId} not found", userId);
            throw new KeyNotFoundException($"User {userId} not found.");
        }

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin {ActorId} attempted admin password reset", _tenantContext.UserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can perform this action.");
        }

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        var updated = await _userRepository.UpdateUserAsync(user);
        
        _logger.LogInformation("SuperAdmin reset password for user {UserId}", userId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    #endregion
}