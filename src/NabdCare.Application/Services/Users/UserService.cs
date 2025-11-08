using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordService passwordService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<UserService> logger,
        IPermissionEvaluator permissionEvaluator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} retrieving user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return null;
        }

        if (!CanAccessUser(user))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to access user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to view this user");
        }

        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task<PaginatedResult<UserResponseDto>> GetAllPagedAsync(
        int limit,
        string? cursor,
        bool includeDeleted = false)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access all users without SuperAdmin role",
                _userContext.GetCurrentUserId());
            throw new UnauthorizedAccessException(
                "Only SuperAdmin can retrieve all users across clinics");
        }

        var abacFilter = new Func<IQueryable<User>, IQueryable<User>>(query =>
            _permissionEvaluator.FilterUsers(query, Common.Constants.Permissions.Users.View, _userContext));

        var result = await _userRepository.GetAllPagedAsync(limit, cursor, includeDeleted, abacFilter);

        return new PaginatedResult<UserResponseDto>
        {
            Items = _mapper.Map<IEnumerable<UserResponseDto>>(result.Items),
            HasMore = result.HasMore,
            NextCursor = result.NextCursor,
            TotalCount = result.TotalCount
        };
    }

    public async Task<PaginatedResult<UserResponseDto>> GetByClinicIdPagedAsync(
        Guid clinicId,
        int limit,
        string? cursor,
        bool includeDeleted = false)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (clinicId == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(clinicId));

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
        {
            _logger.LogWarning(
                "User {UserId} from clinic {UserClinicId} attempted to access clinic {TargetClinicId}",
                currentUserId, _tenantContext.ClinicId, clinicId);
            throw new UnauthorizedAccessException(
                $"You don't have permission to view users from clinic {clinicId}");
        }

        Func<IQueryable<User>, IQueryable<User>> abacFilter = query =>
        {
            if (_tenantContext.IsSuperAdmin)
                return query;

            if (_tenantContext.ClinicId.HasValue)
                return query.Where(u => u.ClinicId == _tenantContext.ClinicId.Value);

            _logger.LogWarning("User {CurrentUserId} has no clinic context for clinic-based query", currentUserId);
            throw new UnauthorizedAccessException("You don't have permission to view this clinic's users");
        };

        var result = await _userRepository.GetByClinicIdPagedAsync(
            clinicId,
            limit,
            cursor,
            includeDeleted,
            abacFilter);

        _logger.LogInformation(
            "User {CurrentUserId} retrieved {Count} users from clinic {ClinicId}",
            currentUserId, result.Items.Count(), clinicId);

        return new PaginatedResult<UserResponseDto>
        {
            Items = _mapper.Map<IEnumerable<UserResponseDto>>(result.Items),
            HasMore = result.HasMore,
            NextCursor = result.NextCursor,
            TotalCount = result.TotalCount
        };
    }

    public async Task<UserResponseDto?> GetCurrentUserAsync()
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            _logger.LogWarning("Invalid current user ID: {CurrentUserId}", currentUserId);
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return await GetUserByIdAsync(userId);
    }

    public async Task<(bool exists, bool isDeleted, Guid? userId)> EmailExistsDetailedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        var currentUserId = _userContext.GetCurrentUserId();

        var hasPermission = await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Create);
        if (!hasPermission)
        {
            _logger.LogWarning(
                "User {UserId} attempted to check email status without Users.Create permission",
                currentUserId);
            throw new UnauthorizedAccessException(
                "You don't have permission to check email status");
        }

        var normalizedEmail = email.Trim().ToLower();
        var user = await _userRepository.GetByEmailIncludingDeletedAsync(normalizedEmail);

        if (user == null)
            return (false, false, null);

        return (true, user.IsDeleted, user.Id);
    }

    public async Task<UserResponseDto?> RestoreUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} restoring user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdForRestorationAsync(id);

        if (user == null || !user.IsDeleted)
            return null;

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to restore user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to restore this user");
        }

        user.IsDeleted = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} restored user {UserId}", currentUserId, id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} creating user {Email}", currentUserId, dto.Email);

        var targetClinicId = await ValidateAndGetTargetClinicId(dto.ClinicId, dto.RoleId);
        await ValidateRoleAssignment(dto.RoleId, targetClinicId);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.Trim().ToLower(),
            FullName = dto.FullName.Trim(),
            RoleId = dto.RoleId,
            ClinicId = targetClinicId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.TryParse(currentUserId, out var uid) ? uid : null,
            IsDeleted = false
        };

        user.PasswordHash = _passwordService.HashPassword(user, dto.Password);

        try
        {
            var created = await _userRepository.CreateAsync(user);

            _logger.LogInformation(
                "User {CurrentUserId} created user {NewUserId} with email {Email}",
                currentUserId, created.Id, created.Email);

            return _mapper.Map<UserResponseDto>(created);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "UX_Users_Email"))
        {
            _logger.LogWarning(
                "Email {Email} already exists. Error code: {ErrorCode}", 
                dto.Email, 
                ErrorCodes.DUPLICATE_EMAIL);

            throw new InvalidOperationException(
                $"A user with email '{dto.Email}' already exists",
                ex);
        }
    }

    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} updating user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for update. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            return null;
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to update user {UserId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You don't have permission to update this user");
        }

        user.FullName = dto.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} updated user {UserId}", currentUserId, id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, Guid roleId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (roleId == Guid.Empty)
            throw new ArgumentException("Role ID cannot be empty", nameof(roleId));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} updating role for user {UserId} to {RoleId}",
            currentUserId, id, roleId);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for role update. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            return null;
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to update role for user {UserId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You don't have permission to update this user's role");
        }

        await ValidateRoleAssignment(roleId, user.ClinicId);

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} updated role for user {UserId} to {RoleId}",
            currentUserId, id, roleId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> ActivateUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} activating user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for activation. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            return null;
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to activate user {UserId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You don't have permission to activate this user");
        }

        if (user.IsActive)
        {
            _logger.LogInformation("User {UserId} is already active", id);
            return _mapper.Map<UserResponseDto>(user);
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} activated user {UserId}", currentUserId, id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> DeactivateUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} deactivating user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for deactivation. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            return null;
        }

        if (user.Id.ToString() == currentUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to deactivate themselves. Error code: {ErrorCode}", 
                currentUserId, 
                ErrorCodes.FORBIDDEN);
            throw new InvalidOperationException("You cannot deactivate your own account");
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to deactivate user {UserId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You don't have permission to deactivate this user");
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("User {UserId} is already inactive", id);
            return _mapper.Map<UserResponseDto>(user);
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} deactivated user {UserId}", currentUserId, id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} soft deleting user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for soft delete. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            return false;
        }

        if (user.Id.ToString() == currentUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to delete themselves. Error code: {ErrorCode}", 
                currentUserId, 
                ErrorCodes.FORBIDDEN);
            throw new InvalidOperationException("You cannot delete your own account");
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to delete user {UserId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You don't have permission to delete this user");
        }

        var result = await _userRepository.SoftDeleteAsync(id);

        if (result)
            _logger.LogInformation("User {CurrentUserId} soft deleted user {UserId}", currentUserId, id);

        return result;
    }

    public async Task<bool> HardDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to hard delete user {UserId}. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete users");
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for hard delete. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            return false;
        }

        if (user.Id.ToString() == currentUserId)
        {
            _logger.LogWarning("SuperAdmin {CurrentUserId} attempted to permanently delete themselves. Error code: {ErrorCode}", 
                currentUserId, 
                ErrorCodes.FORBIDDEN);
            throw new InvalidOperationException("You cannot permanently delete your own account");
        }

        var deleted = await _userRepository.DeleteAsync(id);

        if (deleted)
            _logger.LogWarning("SuperAdmin {CurrentUserId} permanently deleted user {UserId}", currentUserId, id);

        return deleted;
    }

    public async Task<UserResponseDto> ChangePasswordAsync(Guid id, ChangePasswordRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();

        if (id.ToString() != currentUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to change password for user {UserId}. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You can only change your own password");
        }

        _logger.LogInformation("User {UserId} changing password", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for password change. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            throw new KeyNotFoundException($"User {id} not found");
        }

        if (!_passwordService.VerifyPassword(user, dto.CurrentPassword))
        {
            _logger.LogWarning("User {UserId} provided incorrect current password. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.INVALID_PASSWORD);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} changed password", id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> ResetPasswordAsync(Guid id, ResetPasswordRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} resetting password for user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for password reset. Error code: {ErrorCode}", 
                id, 
                ErrorCodes.USER_NOT_FOUND);
            throw new KeyNotFoundException($"User {id} not found");
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to reset password for user {UserId} without permission. Error code: {ErrorCode}",
                currentUserId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You don't have permission to reset this user's password");
        }

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} reset password for user {UserId}", currentUserId, id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    private bool CanAccessUser(User? user)
    {
        if (user == null)
            return false;

        if (_tenantContext.IsSuperAdmin)
            return true;

        return user.ClinicId.HasValue && user.ClinicId == _tenantContext.ClinicId;
    }

    private bool CanManageUser(Guid? targetClinicId)
    {
        if (_tenantContext.IsSuperAdmin)
            return true;

        return targetClinicId.HasValue && targetClinicId == _tenantContext.ClinicId;
    }

    private async Task<Guid?> ValidateAndGetTargetClinicId(Guid? requestedClinicId, Guid roleId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}", 
                roleId, 
                ErrorCodes.ROLE_NOT_FOUND);
            throw new InvalidOperationException($"Role {roleId} does not exist");
        }

        if (_tenantContext.IsSuperAdmin)
        {
            if (role.IsSystemRole)
            {
                if (requestedClinicId.HasValue)
                    throw new ArgumentException("System users must not have a clinic ID");

                return null;
            }

            if (!requestedClinicId.HasValue)
                throw new ArgumentException("SuperAdmin must specify clinic ID when creating clinic users");

            return requestedClinicId.Value;
        }

        if (!_tenantContext.ClinicId.HasValue)
        {
            _logger.LogWarning("User attempting to create user without clinic context. Error code: {ErrorCode}", 
                ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You must belong to a clinic to create users");
        }

        if (requestedClinicId.HasValue && requestedClinicId != _tenantContext.ClinicId)
        {
            _logger.LogWarning("User attempting to create user in different clinic. Error code: {ErrorCode}", 
                ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException("You can only create users in your own clinic");
        }

        return _tenantContext.ClinicId.Value;
    }

    private async Task ValidateRoleAssignment(Guid roleId, Guid? clinicId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found for assignment. Error code: {ErrorCode}", 
                roleId, 
                ErrorCodes.ROLE_NOT_FOUND);
            throw new InvalidOperationException($"Role {roleId} does not exist");
        }

        if (role.IsSystemRole)
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin attempted to assign system role. Error code: {ErrorCode}", 
                    ErrorCodes.FORBIDDEN);
                throw new UnauthorizedAccessException("Only SuperAdmin can assign system roles");
            }

            if (clinicId != null)
                throw new InvalidOperationException("System users must not have a clinic ID");

            return;
        }

        if (!role.IsTemplate && role.ClinicId != clinicId)
        {
            _logger.LogWarning("Role {RoleId} does not belong to clinic {ClinicId}. Error code: {ErrorCode}", 
                roleId, 
                clinicId, 
                ErrorCodes.CONFLICT);
            throw new InvalidOperationException("Role does not belong to the specified clinic");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex, string constraintName)
    {
        var message = ex.InnerException?.Message ?? string.Empty;
        return message.Contains(constraintName, StringComparison.OrdinalIgnoreCase)
               || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
               || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}