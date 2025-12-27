using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
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
        ISubscriptionRepository subscriptionRepository, // ✅ Added to Constructor
        IPasswordService passwordService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<UserService> logger,
        IPermissionEvaluator permissionEvaluator)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _subscriptionRepository =
            subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    // =========================================================================
    // 1. CREATE USER (With Subscription Gatekeeper)
    // =========================================================================
    public async Task<UserResponseDto> CreateUserAsync(CreateUserRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();

        // 1. Resolve Target Clinic
        var targetClinicId = await ValidateAndGetTargetClinicId(dto.ClinicId, dto.RoleId);

        // 2. Validate Role
        await ValidateRoleAssignment(dto.RoleId, targetClinicId);

        // =================================================================
        // 🛑 GATEKEEPER: ENFORCE SUBSCRIPTION LIMITS
        // =================================================================
        if (targetClinicId.HasValue)
        {
            var clinicId = targetClinicId.Value;

            // A. Get Active Subscription
            var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(clinicId);

            if (activeSub == null)
            {
                _logger.LogWarning("Clinic {ClinicId} attempted to create user without active subscription.", clinicId);
                throw new InvalidOperationException(
                    "No active subscription found. Please subscribe to a plan to add users.");
            }

            // B. Check Limits
            var currentUserCount = await _userRepository.CountByClinicIdAsync(clinicId);

            // MaxUsers property automatically sums Included + Purchased + Bonus
            if (currentUserCount >= activeSub.MaxUsers)
            {
                _logger.LogWarning(
                    "Limit Reached: Clinic {ClinicId} has {Count}/{Max} users.", 
                    clinicId, currentUserCount, activeSub.MaxUsers);

                // ✅ UPDATED MESSAGE: Cleaner, actionable, and works for all contexts.
                throw new InvalidOperationException(
                    $"Subscription limit reached ({activeSub.MaxUsers} active users). " +
                    "Please upgrade your plan or purchase additional user seats to create new accounts.");
            }
        }
        // =================================================================

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
            return _mapper.Map<UserResponseDto>(created);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "UX_Users_Email"))
        {
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists", ex);
        }
    }

    // =========================================================================
    // REMAINING METHODS (Unchanged logic, just keeping full file structure)
    // =========================================================================

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return null;

        if (!CanAccessUser(user))
        {
            _logger.LogWarning("User {CurrentUserId} attempted to access user {UserId} without permission",
                currentUserId, id);
            throw new UnauthorizedAccessException("You don't have permission to view this user");
        }

        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task<PaginatedResult<UserResponseDto>> GetAllPagedAsync(int limit, string? cursor,
        bool includeDeleted = false)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin can retrieve all users across clinics");
        }

        var abacFilter = new Func<IQueryable<User>, IQueryable<User>>(query =>
            _permissionEvaluator.FilterUsers(query, Common.Constants.Permissions.Users.View, _userContext));

        var result = await _userRepository.GetAllPagedAsync(limit, cursor, includeDeleted, abacFilter);
        return result.ToPaginatedDto<User, UserResponseDto>(_mapper);
    }

    public async Task<PaginatedResult<UserResponseDto>> GetByClinicIdPagedAsync(Guid clinicId, int limit,
        string? cursor, bool includeDeleted = false)
    {
        var currentUserId = _userContext.GetCurrentUserId();

        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic ID cannot be empty", nameof(clinicId));

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
        {
            throw new UnauthorizedAccessException($"You don't have permission to view users from clinic {clinicId}");
        }

        Func<IQueryable<User>, IQueryable<User>> abacFilter = query =>
        {
            if (_tenantContext.IsSuperAdmin) return query;
            if (_tenantContext.ClinicId.HasValue) return query.Where(u => u.ClinicId == _tenantContext.ClinicId.Value);
            throw new UnauthorizedAccessException("You don't have permission to view this clinic's users");
        };

        var result = await _userRepository.GetByClinicIdPagedAsync(clinicId, limit, cursor, includeDeleted, abacFilter);
        return result.ToPaginatedDto<User, UserResponseDto>(_mapper);
    }

    public async Task<UserResponseDto?> GetCurrentUserAsync()
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return await GetUserByIdAsync(userId);
    }

    public async Task<(bool exists, bool isDeleted, Guid? userId)> EmailExistsDetailedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));

        var hasPermission = await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Users.Create);
        if (!hasPermission) throw new UnauthorizedAccessException("You don't have permission to check email status");

        var user = await _userRepository.GetByEmailIncludingDeletedAsync(email.Trim().ToLower());
        if (user == null) return (false, false, null);

        return (true, user.IsDeleted, user.Id);
    }

    public async Task<UserResponseDto?> RestoreUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdForRestorationAsync(id);

        if (user == null || !user.IsDeleted) return null;

        if (!CanManageUser(user.ClinicId))
        {
            throw new UnauthorizedAccessException("You don't have permission to restore this user");
        }

        // =================================================================
        // 🛑 GATEKEEPER: ENFORCE LIMITS ON RESTORE
        // =================================================================
        if (user.ClinicId.HasValue)
        {
            var clinicId = user.ClinicId.Value;
            var activeSub = await _subscriptionRepository.GetActiveByClinicIdAsync(clinicId);

            if (activeSub == null)
                throw new InvalidOperationException("No active subscription found.");

            var currentUserCount = await _userRepository.CountByClinicIdAsync(clinicId);

            if (currentUserCount >= activeSub.MaxUsers)
            {
                _logger.LogWarning("Limit Reached during Restore: Clinic {Id} has {Count}/{Max}", clinicId, currentUserCount, activeSub.MaxUsers);
             
                throw new InvalidOperationException(
                    $"Cannot restore user. Subscription limit reached ({activeSub.MaxUsers} active users). " +
                    "Please upgrade your plan or delete another user first.");
            }
        }
        // =================================================================

        user.IsDeleted = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return null;

        if (!CanManageUser(user.ClinicId))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this user");
        }

        user.FullName = dto.FullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> UpdateUserRoleAsync(Guid id, Guid roleId)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID cannot be empty", nameof(roleId));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return null;

        if (!CanManageUser(user.ClinicId))
        {
            throw new UnauthorizedAccessException("You don't have permission to update this user's role");
        }

        await ValidateRoleAssignment(roleId, user.ClinicId);

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> ActivateUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return null;

        if (!CanManageUser(user.ClinicId))
        {
            throw new UnauthorizedAccessException("You don't have permission to activate this user");
        }

        if (user.IsActive) return _mapper.Map<UserResponseDto>(user);

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto?> DeactivateUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return null;

        if (user.Id.ToString() == currentUserId)
            throw new InvalidOperationException("You cannot deactivate your own account");
        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("You don't have permission to deactivate this user");

        if (!user.IsActive) return _mapper.Map<UserResponseDto>(user);

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<bool> SoftDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return false;
        if (user.Id.ToString() == currentUserId)
            throw new InvalidOperationException("You cannot delete your own account");
        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("You don't have permission to delete this user");

        return await _userRepository.SoftDeleteAsync(id);
    }

    public async Task<bool> HardDeleteUserAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));

        var currentUserId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can permanently delete users");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;
        if (user.Id.ToString() == currentUserId)
            throw new InvalidOperationException("You cannot permanently delete your own account");

        return await _userRepository.DeleteAsync(id);
    }

    public async Task<UserResponseDto> ChangePasswordAsync(Guid id, ChangePasswordRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        if (id.ToString() != currentUserId)
            throw new UnauthorizedAccessException("You can only change your own password");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new KeyNotFoundException($"User {id} not found");

        if (!_passwordService.VerifyPassword(user, dto.CurrentPassword))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

        var updated = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDto>(updated);
    }

    public async Task<UserResponseDto> ResetPasswordAsync(Guid id, ResetPasswordRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var currentUserId = _userContext.GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new KeyNotFoundException($"User {id} not found");

        if (!CanManageUser(user.ClinicId))
            throw new UnauthorizedAccessException("You don't have permission to reset this user's password");

        user.PasswordHash = _passwordService.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUserId;

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
        if (role == null) throw new InvalidOperationException($"Role {roleId} does not exist");

        if (_tenantContext.IsSuperAdmin)
        {
            if (role.IsSystemRole)
            {
                if (requestedClinicId.HasValue) throw new ArgumentException("System users must not have a clinic ID");
                return null;
            }

            if (!requestedClinicId.HasValue)
                throw new ArgumentException("SuperAdmin must specify clinic ID when creating clinic users");
            return requestedClinicId.Value;
        }

        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to create users");
        if (requestedClinicId.HasValue && requestedClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only create users in your own clinic");

        return _tenantContext.ClinicId.Value;
    }

    private async Task ValidateRoleAssignment(Guid roleId, Guid? clinicId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) throw new InvalidOperationException($"Role {roleId} does not exist");

        if (role.IsSystemRole)
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only SuperAdmin can assign system roles");
            if (clinicId != null) throw new InvalidOperationException("System users must not have a clinic ID");
            return;
        }

        if (!role.IsTemplate && role.ClinicId != clinicId)
        {
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