using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Services.Users;

/// <summary>
/// Production-ready user service with comprehensive error handling, audit logging,
/// multi-tenant security, and clean architecture principles.
/// </summary>
public class UserService : IUserService
{
        private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordService passwordService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

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

        // Multi-tenant security check
        if (!CanAccessUser(user))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to access user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to view this user");
        }

        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task<PaginatedResult<UserResponseDto>> GetAllPagedAsync(int limit, string? cursor)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to access GetAllPagedAsync without SuperAdmin rights", currentUserId);
            throw new UnauthorizedAccessException("Only SuperAdmin can view all users");
        }

        var result = await _userRepository.GetAllPagedAsync(limit, cursor);

        _logger.LogInformation("SuperAdmin {CurrentUserId} retrieved {Count} users (HasMore={HasMore})",
            currentUserId, result.Items.Count(), result.HasMore);

        return new PaginatedResult<UserResponseDto>
        {
            Items = _mapper.Map<IEnumerable<UserResponseDto>>(result.Items),
            HasMore = result.HasMore,
            NextCursor = result.NextCursor,
            TotalCount = result.TotalCount
        };
    }

    public async Task<PaginatedResult<UserResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, int limit, string? cursor)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (clinicId == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty", nameof(clinicId));

        // Access control:
        if (_tenantContext.IsSuperAdmin)
        {
            // ok
        }
        else if (_tenantContext.ClinicId.HasValue && _tenantContext.ClinicId.Value == clinicId)
        {
            // ok
        }
        else
        {
            _logger.LogWarning("User {CurrentUserId} attempted to query clinic {ClinicId} without permission",
                currentUserId, clinicId);
            throw new UnauthorizedAccessException("You don't have permission to view this clinic's users");
        }

        var result = await _userRepository.GetByClinicIdPagedAsync(clinicId, limit, cursor);

        _logger.LogInformation("User {CurrentUserId} retrieved {Count} users from clinic {ClinicId} (HasMore={HasMore})",
            currentUserId, result.Items.Count(), clinicId, result.HasMore);

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

    #endregion

    #region COMMAND METHODS

    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        ValidateUserCreationDto(dto);

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} creating user {Email}", currentUserId, dto.Email);

        var targetClinicId = await ValidateAndGetTargetClinicId(dto.ClinicId, dto.RoleId);

        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            _logger.LogWarning("Attempted to create user with duplicate email: {Email}", dto.Email);
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists");
        }

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
            CreatedBy = currentUserId,
            IsDeleted = false
        };

        user.PasswordHash = _passwordService.HashPassword(user, dto.Password);

        var created = await _userRepository.CreateAsync(user);

        _logger.LogInformation("User {CurrentUserId} successfully created user {NewUserId} ({Email}) in clinic {ClinicId}",
            currentUserId, created.Id, created.Email, targetClinicId);

        return _mapper.Map<UserResponseDto>(created);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ArgumentException("Full name is required", nameof(dto.FullName));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} updating user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for update", id);
            return null;
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to update user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to update this user");
        }

        user.FullName = dto.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} successfully updated user {UserId}", currentUserId, id);

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
            _logger.LogWarning("User {UserId} not found for role update", id);
            return null;
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to update role for user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to update this user's role");
        }

        await ValidateRoleAssignment(roleId, user.ClinicId);

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} successfully updated role for user {UserId} to {RoleId}",
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
            _logger.LogWarning("User {UserId} not found for activation", id);
            return null;
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to activate user {UserId} without permission",
                currentUserId, id);
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

        _logger.LogInformation("User {CurrentUserId} successfully activated user {UserId} ({Email})",
            currentUserId, id, user.Email);

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
            _logger.LogWarning("User {UserId} not found for deactivation", id);
            return null;
        }

        if (user.Id.ToString() == currentUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to deactivate themselves", currentUserId);
            throw new InvalidOperationException("You cannot deactivate your own account");
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to deactivate user {UserId} without permission",
                currentUserId, id);
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

        _logger.LogInformation("User {CurrentUserId} successfully deactivated user {UserId} ({Email})",
            currentUserId, id, user.Email);

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
            _logger.LogWarning("User {UserId} not found for soft delete", id);
            return false;
        }

        if (user.Id.ToString() == currentUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to delete themselves", currentUserId);
            throw new InvalidOperationException("You cannot delete your own account");
        }

        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to delete user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to delete this user");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = currentUserId;

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {CurrentUserId} successfully soft deleted user {UserId} ({Email})",
            currentUserId, id, user.Email);

        return true;
    }

    public async Task<bool> HardDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted to hard delete user {UserId}",
                currentUserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete users");
        }

        _logger.LogWarning("SuperAdmin {CurrentUserId} permanently deleting user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for hard delete", id);
            return false;
        }

        if (user.Id.ToString() == currentUserId)
        {
            _logger.LogWarning("SuperAdmin {CurrentUserId} attempted to permanently delete themselves", currentUserId);
            throw new InvalidOperationException("You cannot permanently delete your own account");
        }

        var deleted = await _userRepository.DeleteAsync(id);

        if (deleted)
        {
            _logger.LogWarning("⚠️ SuperAdmin {CurrentUserId} PERMANENTLY DELETED user {UserId} ({Email})",
                currentUserId, id, user.Email);
        }

        return deleted;
    }

    #endregion

    #region PASSWORD MANAGEMENT

    public async Task<UserResponseDto> ChangePasswordAsync(Guid id, ChangePasswordRequestDto dto)
    {
        // Input validation
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        
        // Users can only change their own password
        if (id.ToString() != currentUserId)
        {
            _logger.LogWarning("User {CurrentUserId} attempted to change password for user {UserId}", 
                currentUserId, id);
            throw new UnauthorizedAccessException("You can only change your own password");
        }

        _logger.LogInformation("User {UserId} changing their password", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for password change", id);
            throw new KeyNotFoundException($"User {id} not found");
        }

        // Verify current password
        if (!_passwordService.VerifyPassword(user, dto.CurrentPassword))
        {
            _logger.LogWarning("User {UserId} provided incorrect current password", id);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Hash new password
        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User {UserId} successfully changed their password", id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> ResetPasswordAsync(Guid id, ResetPasswordRequestDto dto)
    {
        // Input validation
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {CurrentUserId} resetting password for user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for password reset", id);
            throw new KeyNotFoundException($"User {id} not found");
        }

        // Multi-tenant authorization check
        if (!CanManageUser(user.ClinicId))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to reset password for user {UserId} without permission", 
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to reset this user's password");
        }

        // Hash new password
        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User {CurrentUserId} successfully reset password for user {UserId}", 
            currentUserId, id);

        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> AdminResetPasswordAsync(Guid id, ResetPasswordRequestDto dto)
    {
        // Input validation
        if (id == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();

        // Only SuperAdmin can use admin reset
        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {CurrentUserId} attempted admin password reset for user {UserId}", 
                currentUserId, id);
            throw new UnauthorizedAccessException("Only SuperAdmin can use admin password reset");
        }

        _logger.LogWarning("SuperAdmin {CurrentUserId} resetting password for user {UserId}", currentUserId, id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for admin password reset", id);
            throw new KeyNotFoundException($"User {id} not found");
        }

        // Hash new password
        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        
        _logger.LogWarning("⚠️ SuperAdmin {CurrentUserId} reset password for user {UserId} ({Email}) in clinic {ClinicId}", 
            currentUserId, id, user.Email, user.ClinicId);

        return _mapper.Map<UserResponseDto>(updated);
    }

    #endregion

    #region HELPER METHODS

    /// <summary>
    /// Check if current user can access the specified user
    /// </summary>
    private bool CanAccessUser(User? user)
    {
        if (user == null)
            return false;

        // SuperAdmin can access all users
        if (_tenantContext.IsSuperAdmin)
            return true;

        // ClinicAdmin can only access users in their clinic
        return user.ClinicId.HasValue && user.ClinicId == _tenantContext.ClinicId;
    }

    /// <summary>
    /// Check if current user can manage users in the specified clinic
    /// </summary>
    private bool CanManageUser(Guid? targetClinicId)
    {
        // SuperAdmin can manage all users
        if (_tenantContext.IsSuperAdmin)
            return true;

        // ClinicAdmin can only manage users in their clinic
        return targetClinicId.HasValue && targetClinicId == _tenantContext.ClinicId;
    }

    /// <summary>
    /// Validate user creation DTO
    /// </summary>
    private void ValidateUserCreationDto(CreateUserRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required", nameof(dto.Email));

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password is required", nameof(dto.Password));

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ArgumentException("Full name is required", nameof(dto.FullName));

        if (dto.RoleId == Guid.Empty)
            throw new ArgumentException("Role ID is required", nameof(dto.RoleId));
    }

    /// <summary>
    /// Validate and get target clinic ID for user creation
    /// </summary>
    private async Task<Guid?> ValidateAndGetTargetClinicId(Guid? requestedClinicId, Guid roleId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} does not exist");

        if (_tenantContext.IsSuperAdmin)
        {
            if (role.IsSystemRole)
            {
                // For system roles, clinicId must be null
                if (requestedClinicId.HasValue)
                    throw new ArgumentException("System users must not have a clinic ID");

                return null;
            }
            else
            {
                // For clinic/template roles, clinicId is required
                if (!requestedClinicId.HasValue)
                    throw new ArgumentException("SuperAdmin must specify clinic ID when creating clinic users");

                return requestedClinicId.Value;
            }
        }
    
        // ClinicAdmin can only create users in their own clinic
        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to create users");

        if (requestedClinicId.HasValue && requestedClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only create users in your own clinic");

        return _tenantContext.ClinicId.Value;
    }

    /// <summary>
    /// Validate role assignment
    /// </summary>
    private async Task ValidateRoleAssignment(Guid roleId, Guid? clinicId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
            throw new InvalidOperationException($"Role {roleId} does not exist");

        if (role.IsSystemRole)
        {
            // Only SuperAdmin can assign system roles, and only for system users (clinicId == null)
            if (!_tenantContext.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only SuperAdmin can assign system roles");

            if (clinicId != null)
                throw new InvalidOperationException("System users must not have a clinic ID");

            // All good: SuperAdmin creating a system user
            return;
        }

        // For non-system roles:
        // Role must belong to the same clinic (or be a template)
        if (!role.IsTemplate && role.ClinicId != clinicId)
            throw new InvalidOperationException("Role does not belong to the specified clinic");
    }

    #endregion
}