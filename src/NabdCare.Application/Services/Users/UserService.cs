using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ISubscriptionRepository subscriptionRepository,
        IPasswordService passwordService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<UserService> logger,
        IPermissionEvaluator permissionEvaluator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    // =========================================================================
    // 1. CREATE USER (With Subscription & Permission Gatekeeper)
    // =========================================================================
    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin)
        {
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Create))
                throw new UnauthorizedAccessException("You lack permission to create users.");
        }

        var currentUserId = _userContext.GetCurrentUserId();

        // 0. Proactive Duplicate Check
        if (await _userRepository.EmailExistsIncludingDeletedAsync(dto.Email.Trim()))
        {
            throw new DomainException($"User with email '{dto.Email}' already exists.", ErrorCodes.DUPLICATE_EMAIL, "Email");
        }

        // 1. Resolve Target Clinic
        var targetClinicId = await ValidateAndGetTargetClinicId(dto.ClinicId, dto.RoleId);

        // 2. Validate Role Integrity
        await ValidateRoleAssignment(dto.RoleId, targetClinicId);

        // =================================================================
        // 🛑 GATEKEEPER: ENFORCE SUBSCRIPTION LIMITS
        // =================================================================
        if (targetClinicId.HasValue)
        {
            var clinicId = targetClinicId.Value;
            var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(clinicId);

            if (activeSub == null)
            {
                _logger.LogWarning("Clinic {ClinicId} attempted to create user without active subscription.", clinicId);
                throw new DomainException("No active subscription found. Please subscribe to a plan to add users.", ErrorCodes.SUBSCRIPTION_REQUIRED);
            }

            var currentUserCount = await _userRepository.CountByClinicIdAsync(clinicId);

            if (currentUserCount >= activeSub.MaxUsers)
            {
                _logger.LogWarning("Limit Reached: Clinic {ClinicId} has {Count}/{Max} users.", clinicId, currentUserCount, activeSub.MaxUsers);
                throw new DomainException($"User limit reached ({activeSub.MaxUsers}). Upgrade plan or buy seats to add more.", ErrorCodes.LIMIT_EXCEEDED, "Limit");
            }
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            FullName = dto.FullName.Trim(),
            RoleId = dto.RoleId,
            ClinicId = targetClinicId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.TryParse(currentUserId, out var uid) ? uid : null,
            IsDeleted = false
        };

        user.PasswordHash = _passwordService.HashPassword(user, dto.Password);

        var created = await _userRepository.CreateAsync(user);
        _logger.LogInformation("User {UserId} created successfully", created.Id);
        
        return _mapper.Map<UserResponseDto>(created);
    }

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        // 🔐 Security: System Admin OR Owner of Clinic OR Self
        var isSelf = user.Id.ToString() == _userContext.GetCurrentUserId();
        if (!isSelf && !CanAccessUser(user))
             throw new UnauthorizedAccessException("You don't have permission to view this user.");

        // 🔐 Permission Check (if not self)
        if (!isSelf && !_tenantContext.IsSuperAdmin)
        {
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.ViewDetails))
                 throw new UnauthorizedAccessException("You lack permissions to view user details.");
        }

        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task<PaginatedResult<UserResponseDto>> GetAllPagedAsync(UserFilterRequestDto filter)
    {
        // 1. Enforce Clinic Scoping (Security)
        if (!_tenantContext.IsSuperAdmin)
        {
            // Non-SuperAdmins MUST be restricted to their own clinic
            // We override whatever the frontend sent
            filter.ClinicId = _tenantContext.ClinicId; 

            // Check permission
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.View))
                throw new UnauthorizedAccessException("You lack permissions to view users.");
        }
    
        // 2. Define ABAC Filter (Fine-grained row security if needed)
        // Even if filter.ClinicId is set above, ABAC acts as a safety net
        Func<IQueryable<User>, IQueryable<User>> abacFilter = query =>
        {
            return _permissionEvaluator.FilterUsers(query, Common.Constants.Permissions.Users.ViewAll, _userContext);
        };

        // 3. Call Repository
        var result = await _userRepository.GetAllPagedAsync(filter, abacFilter);
        return result.ToPaginatedDto<User, UserResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<UserResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, int limit, string? cursor, bool includeDeleted = false)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic ID required", nameof(clinicId));

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
            throw new UnauthorizedAccessException("You don't have permission to view users from this clinic.");

        // 🔐 Permission Check: Must have 'Users.View'
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.View))
             throw new UnauthorizedAccessException("You lack permissions to view users.");

        Func<IQueryable<User>, IQueryable<User>> abacFilter = query =>
        {
            if (_tenantContext.IsSuperAdmin) return query;
            return query.Where(u => u.ClinicId == _tenantContext.ClinicId.Value);
        };

        var result = await _userRepository.GetByClinicIdPagedAsync(clinicId, limit, cursor, includeDeleted, abacFilter);
        return result.ToPaginatedDto<User, UserResponseDto>(_mapper);
    }

    public async Task<UserResponseDto?> GetCurrentUserAsync()
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated");

        return await GetUserByIdAsync(userId);
    }

    public async Task<(bool exists, bool isDeleted, Guid? userId)> EmailExistsDetailedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required", nameof(email));
        // Typically public or low-privilege
        var user = await _userRepository.GetByEmailIncludingDeletedAsync(email.Trim().ToLowerInvariant());
        if (user == null) return (false, false, null);
        return (true, user.IsDeleted, user.Id);
    }

    // =========================================================================
    // COMMAND METHODS
    // =========================================================================

    public async Task<UserResponseDto?> RestoreUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var user = await _userRepository.GetByIdForRestorationAsync(id);
        if (user == null || !user.IsDeleted) return null;

        // 🔐 Permission: Users.Restore
        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("You don't have permission to restore this user.");
            
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Restore))
             throw new UnauthorizedAccessException("You lack permissions to restore users.");

        // 🛑 GATEKEEPER
        if (user.ClinicId.HasValue)
        {
            var clinicId = user.ClinicId.Value;
            var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(clinicId);

            if (activeSub == null) throw new DomainException("No active subscription found.", ErrorCodes.SUBSCRIPTION_REQUIRED);

            var count = await _userRepository.CountByClinicIdAsync(clinicId);
            if (count >= activeSub.MaxUsers)
                throw new DomainException($"Cannot restore user. Limit reached ({activeSub.MaxUsers}).", ErrorCodes.LIMIT_EXCEEDED);
        }

        user.IsDeleted = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _userContext.GetCurrentUserId();

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        // 🔐 Allow Self-Update OR Permission Update
        var isSelf = user.Id.ToString() == _userContext.GetCurrentUserId();
        if (!isSelf)
        {
            if (!CanManageUser(user.ClinicId))
                throw new UnauthorizedAccessException("Access denied.");
                
            if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Edit))
                 throw new UnauthorizedAccessException("You lack permissions to edit other users.");
        }

        user.FullName = dto.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _userContext.GetCurrentUserId();

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, Guid roleId)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));

        // 🔐 Permission: Users.ChangeRole
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.ChangeRole))
             throw new UnauthorizedAccessException("You lack permissions to change user roles.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("Access denied.");

        await ValidateRoleAssignment(roleId, user.ClinicId);

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _userContext.GetCurrentUserId();

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> ActivateUserAsync(Guid id)
    {
        return await SetUserActiveStatusAsync(id, true);
    }

    public async Task<UserResponseDto?> DeactivateUserAsync(Guid id)
    {
        return await SetUserActiveStatusAsync(id, false);
    }

    private async Task<UserResponseDto?> SetUserActiveStatusAsync(Guid id, bool isActive)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        // 🔐 Permission: Users.Activate
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Activate))
             throw new UnauthorizedAccessException($"You lack permissions to {(isActive ? "activate" : "deactivate")} users.");

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        if (!isActive && user.Id.ToString() == currentUserId)
             throw new DomainException("You cannot deactivate your own account.", ErrorCodes.INVALID_OPERATION);

        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("Access denied.");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        // 🔐 Permission: Users.Delete
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Delete))
             throw new UnauthorizedAccessException("You lack permissions to delete users.");

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        if (user.Id.ToString() == currentUserId)
            throw new DomainException("You cannot delete your own account.", ErrorCodes.INVALID_OPERATION);

        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("Access denied.");

        return await _userRepository.SoftDeleteAsync(id);
    }

    public async Task<bool> HardDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        // 🔐 Strict Permission: Users.HardDelete (Usually SuperAdmin only)
        if (!_tenantContext.IsSuperAdmin)
        {
             // Even if they have the permission, we might want to restrict this further
             if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.HardDelete))
                 throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete users.");
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        if (user.Id.ToString() == _userContext.GetCurrentUserId())
             throw new DomainException("You cannot permanently delete your own account.", ErrorCodes.INVALID_OPERATION);

        return await _userRepository.DeleteAsync(id);
    }

    public async Task<UserResponseDto> ChangePasswordAsync(Guid id, ChangePasswordRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        
        // Self-Service Only (Use ResetPassword for admins)
        if (id.ToString() != currentUserId)
            throw new UnauthorizedAccessException("You can only change your own password.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new KeyNotFoundException($"User {id} not found");

        if (!_passwordService.VerifyPassword(user, dto.CurrentPassword))
            throw new DomainException("Current password is incorrect.", ErrorCodes.INVALID_CREDENTIALS);

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> ResetPasswordAsync(Guid id, ResetPasswordRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        
        // 🔐 Permission: Users.ResetPassword
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.ResetPassword))
             throw new UnauthorizedAccessException("You lack permissions to reset passwords.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new KeyNotFoundException($"User {id} not found");

        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("Permission denied.");

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _userContext.GetCurrentUserId();

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    // ============================================
    // PRIVATE HELPERS
    // ============================================

    private bool CanAccessUser(User? user)
    {
        if (user == null) return false;
        if (_tenantContext.IsSuperAdmin) return true;
        return user.ClinicId.HasValue && user.ClinicId == _tenantContext.ClinicId;
    }

    private bool CanManageUser(Guid? targetClinicId)
    {
        if (_tenantContext.IsSuperAdmin) return true;
        return targetClinicId.HasValue && targetClinicId == _tenantContext.ClinicId;
    }

    private async Task<Guid?> ValidateAndGetTargetClinicId(Guid? requestedClinicId, Guid roleId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) 
            throw new DomainException($"Role {roleId} does not exist.", ErrorCodes.ROLE_NOT_FOUND);

        if (_tenantContext.IsSuperAdmin)
        {
            if (role.IsSystemRole)
            {
                if (requestedClinicId.HasValue) 
                    throw new DomainException("System users must not have a clinic ID.", ErrorCodes.INVALID_OPERATION);
                return null;
            }

            if (!requestedClinicId.HasValue)
                throw new DomainException("SuperAdmin must specify clinic ID for clinic users.", ErrorCodes.INVALID_ARGUMENT);
            
            return requestedClinicId.Value;
        }

        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to create users.");
            
        if (requestedClinicId.HasValue && requestedClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only create users in your own clinic.");

        return _tenantContext.ClinicId.Value;
    }

    private async Task ValidateRoleAssignment(Guid roleId, Guid? clinicId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) 
            throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);

        if (role.IsSystemRole)
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only SuperAdmin can assign system roles.");
            if (clinicId != null) 
                throw new DomainException("System users cannot be assigned to a clinic.", ErrorCodes.INVALID_OPERATION);
            return;
        }

        if (!role.IsTemplate && role.ClinicId != clinicId)
        {
            throw new DomainException("Role does not belong to the target clinic.", ErrorCodes.INVALID_OPERATION);
        }
    }
}