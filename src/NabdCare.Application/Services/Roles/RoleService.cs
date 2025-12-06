using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Application.Services.Roles;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IValidator<CreateRoleRequestDto> _createRoleValidator;
    private readonly IValidator<UpdateRoleRequestDto> _updateRoleValidator;
    private readonly IValidator<CloneRoleRequestDto> _cloneRoleValidator;

    public RoleService(
        IRoleRepository roleRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<RoleService> logger,
        IPermissionEvaluator permissionEvaluator,
        IValidator<CreateRoleRequestDto> createRoleValidator,
        IValidator<UpdateRoleRequestDto> updateRoleValidator,
        IValidator<CloneRoleRequestDto> cloneRoleValidator)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
        _createRoleValidator = createRoleValidator ?? throw new ArgumentNullException(nameof(createRoleValidator));
        _updateRoleValidator = updateRoleValidator ?? throw new ArgumentNullException(nameof(updateRoleValidator));
        _cloneRoleValidator = cloneRoleValidator ?? throw new ArgumentNullException(nameof(cloneRoleValidator));
    }

    // ============================================
    // QUERY METHODS
    // ============================================

    /// <summary>
    /// Get all roles with optional filtering by deletion status and clinic.
    /// </summary>
    public async Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync(bool includeDeleted = false, Guid? clinicId = null)
    {
        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} getting all roles (includeDeleted={IncludeDeleted}, clinicId={ClinicId})",
            userId, includeDeleted, clinicId ?? Guid.Empty);

        var roles = await _roleRepository.GetAllRolesAsync(includeDeleted, clinicId);
        var roleList = roles.ToList();

        _logger.LogInformation("User {UserId} retrieved {Count} roles", userId, roleList.Count);
        return await MapRolesToDtos(roleList);
    }

    public async Task<PaginatedResult<RoleResponseDto>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includeDeleted = false)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation(
            "User {UserId} requested paginated roles (includeDeleted={IncludeDeleted}, Limit={Limit})",
            userId, includeDeleted, pagination.Limit);

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning(
                "Non-SuperAdmin user {UserId} attempted to access all paginated roles. Error code: {ErrorCode}",
                userId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"Only SuperAdmin can view all roles. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var result = await _roleRepository.GetAllPagedAsync(pagination, includeDeleted, query =>
            _permissionEvaluator.FilterRoles(query, "Roles.View", _userContext));

        var items = await MapRolesToDtos(result.Items);

        var roleResponseDtos = items.ToList();
        _logger.LogInformation("User {UserId} retrieved {Count} paginated roles", userId, roleResponseDtos.Count());

        return new PaginatedResult<RoleResponseDto>
        {
            Items = roleResponseDtos,
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<PaginatedResult<RoleResponseDto>> GetClinicRolesPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includeDeleted = false)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(clinicId));

        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation(
            "User {UserId} requested paginated roles for clinic {ClinicId} (includeDeleted={IncludeDeleted})",
            userId, clinicId, includeDeleted);

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to view roles for clinic {ClinicId} without permission. Error code: {ErrorCode}",
                userId, clinicId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You can only view roles for your own clinic. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var result = await _roleRepository.GetClinicRolesPagedAsync(clinicId, pagination, includeDeleted, query =>
            _permissionEvaluator.FilterRoles(query, "Roles.View", _userContext));

        var items = await MapRolesToDtos(result.Items);

        var roleResponseDtos = items as RoleResponseDto[] ?? items.ToArray();
        _logger.LogInformation("User {UserId} retrieved {Count} roles for clinic {ClinicId}", userId,
            roleResponseDtos.Length, clinicId);

        return new PaginatedResult<RoleResponseDto>
        {
            Items = roleResponseDtos,
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<IEnumerable<RoleResponseDto>> GetSystemRolesAsync()
    {
        var userId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to view system roles. Error code: {ErrorCode}",
                userId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"Only SuperAdmin can view system roles. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var roles = await _roleRepository.GetSystemRolesAsync();
        var enumerable = roles.ToList();
        _logger.LogInformation("SuperAdmin {UserId} retrieved {Count} system roles", userId, enumerable.Count);
        return await MapRolesToDtos(enumerable);
    }

    public async Task<IEnumerable<RoleResponseDto>> GetTemplateRolesAsync()
    {
        var roles = await _roleRepository.GetTemplateRolesAsync();
        var enumerable = roles.ToList();
        _logger.LogInformation("Retrieved {Count} template roles", enumerable.Count);
        return await MapRolesToDtos(enumerable);
    }

    public async Task<IEnumerable<RoleResponseDto>> GetClinicRolesAsync(Guid clinicId)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException($"Clinic ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(clinicId));

        var userId = _userContext.GetCurrentUserId();

        if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to view roles for clinic {ClinicId} without permission. Error code: {ErrorCode}",
                userId, clinicId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You can only view roles for your own clinic. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var roles = (await _roleRepository.GetClinicRolesPagedAsync(clinicId, new PaginationRequestDto { Limit = 100 }))
            .Items;
        var enumerable = roles.ToList();
        _logger.LogInformation("User {UserId} retrieved {Count} roles for clinic {ClinicId}", userId, enumerable.Count,
            clinicId);
        return await MapRolesToDtos(enumerable);
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(id));

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}", id, ErrorCodes.ROLE_NOT_FOUND);
            return null;
        }

        if (!CanAccessRole(role))
        {
            _logger.LogWarning(
                "User {UserId} attempted to view role {RoleId} without permission. Error code: {ErrorCode}",
                _userContext.GetCurrentUserId(), id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You don't have permission to view this role. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var dto = _mapper.Map<RoleResponseDto>(role);
        dto.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        dto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

        _logger.LogInformation("Retrieved role {RoleId}", id);
        return dto;
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(roleId));

        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}", roleId, ErrorCodes.ROLE_NOT_FOUND);
            throw new KeyNotFoundException($"Role {roleId} not found. Error code: {ErrorCodes.ROLE_NOT_FOUND}");
        }

        if (!CanAccessRole(role))
        {
            _logger.LogWarning(
                "User {UserId} attempted to view permissions for role {RoleId} without permission. Error code: {ErrorCode}",
                _userContext.GetCurrentUserId(), roleId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You don't have permission to view this role's permissions. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(roleId);
        var enumerable = permissionIds.ToList();
        _logger.LogInformation("Retrieved {Count} permissions for role {RoleId}", enumerable.Count, roleId);
        return enumerable.Select(p => p.ToString());
    }

    // ============================================
    // COMMAND METHODS
    // ============================================

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Validate DTO
        var validationResult = await _createRoleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Role creation validation failed. Errors: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} creating role {RoleName}", userId, dto.Name);

        var targetClinicId = await ValidateCreateRolePermissions(dto);

        if (await _roleRepository.RoleNameExistsAsync(dto.Name, targetClinicId))
        {
            _logger.LogWarning("Role name {RoleName} already exists. Error code: {ErrorCode}",
                dto.Name, ErrorCodes.DUPLICATE_RESOURCE);
            throw new InvalidOperationException(
                $"Role name '{dto.Name}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");
        }

        var role = _mapper.Map<Role>(dto);
        role.Id = Guid.NewGuid();
        role.ClinicId = targetClinicId;
        role.CreatedAt = DateTime.UtcNow;
        role.CreatedBy = userId;

        var created = await _roleRepository.CreateRoleAsync(role);

        if (dto.TemplateRoleId.HasValue)
            await CopyPermissionsFromTemplate(created.Id, dto.TemplateRoleId.Value);

        var response = _mapper.Map<RoleResponseDto>(created);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);

        _logger.LogInformation("User {UserId} created role {RoleId} with name {RoleName}", userId, created.Id,
            created.Name);
        return response;
    }

    public async Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, CloneRoleRequestDto dto)
    {
        if (templateRoleId == Guid.Empty)
            throw new ArgumentException("Template role ID cannot be empty.", nameof(templateRoleId));

        var validationResult = await _cloneRoleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = _userContext.GetCurrentUserId();
        var templateRole = await _roleRepository.GetRoleByIdAsync(templateRoleId);

        if (templateRole == null)
            throw new KeyNotFoundException(
                $"Template role {templateRoleId} not found. Error code: {ErrorCodes.ROLE_NOT_FOUND}");

        // 1. Determine Target Scope (Allow null for SuperAdmin global templates)
        var targetClinicId = await ValidateCloneRolePermissions(dto.ClinicId);

        // 2. Determine Role Type
        bool isSystemRole = false;
        bool isTemplate = false;

        if (targetClinicId == null)
        {
            // Global Scope logic
            if (templateRole.IsSystemRole)
                isSystemRole = true;
            else
                isTemplate = true;
        }

        // 3. Validate Name
        var roleName = string.IsNullOrWhiteSpace(dto.NewRoleName)
            ? $"{templateRole.Name} (Copy)"
            : dto.NewRoleName.Trim();

        if (await _roleRepository.RoleNameExistsAsync(roleName, targetClinicId))
            throw new InvalidOperationException(
                $"Role '{roleName}' already exists. Error code: {ErrorCodes.DUPLICATE_RESOURCE}");

        // 4. Create Entity
        var clonedRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            ClinicId = targetClinicId,

            // Visuals: Prefer DTO, fallback to Source
            Description = !string.IsNullOrWhiteSpace(dto.Description) ? dto.Description : templateRole.Description,
            ColorCode = !string.IsNullOrWhiteSpace(dto.ColorCode) ? dto.ColorCode : templateRole.ColorCode,
            IconClass = !string.IsNullOrWhiteSpace(dto.IconClass) ? dto.IconClass : templateRole.IconClass,
            DisplayOrder = dto.DisplayOrder ?? templateRole.DisplayOrder,

            // Type Flags
            IsSystemRole = isSystemRole,
            IsTemplate = isTemplate,

            TemplateRoleId = templateRoleId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        var created = await _roleRepository.CreateRoleAsync(clonedRole);

        // 5. Copy Permissions
        if (dto.CopyPermissions)
        {
            await CopyPermissionsFromTemplate(created.Id, templateRoleId);
        }

        var response = _mapper.Map<RoleResponseDto>(created);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);

        _logger.LogInformation("User {UserId} cloned role {NewRoleId} from {TemplateRoleId}", userId, created.Id,
            templateRoleId);

        return response;
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Validate DTO
        var validationResult = await _updateRoleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Role update validation failed. Errors: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} updating role {RoleId}", userId, id);

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found for update. Error code: {ErrorCode}",
                id, ErrorCodes.ROLE_NOT_FOUND);
            return null;
        }

        if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("User {UserId} attempted to update system role {RoleId}. Error code: {ErrorCode}",
                userId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"Cannot update system roles. Error code: {ErrorCodes.FORBIDDEN}");
        }

        if (!CanAccessRole(role))
        {
            _logger.LogWarning(
                "User {UserId} attempted to update role {RoleId} without permission. Error code: {ErrorCode}",
                userId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You don't have permission to update this role. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _mapper.Map(dto, role);
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = userId;

        var updated = await _roleRepository.UpdateRoleAsync(role);
        if (updated == null)
        {
            _logger.LogWarning("Failed to update role {RoleId}. Error code: {ErrorCode}", id,
                ErrorCodes.INTERNAL_ERROR);
            return null;
        }

        var response = _mapper.Map<RoleResponseDto>(updated);
        response.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

        _logger.LogInformation("User {UserId} updated role {RoleId}", userId, id);
        return response;
    }

    // ---------------------------------------------------------
// SOFT DELETE (Move to Trash)
// ---------------------------------------------------------
    public async Task<bool> SoftDeleteRoleAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(id));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} soft deleting role {RoleId}", userId, id);

        // 1. Get Role
        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found. Error code: {ErrorCode}", id, ErrorCodes.ROLE_NOT_FOUND);
            throw new KeyNotFoundException($"Role with ID {id} not found. Error code: {ErrorCodes.ROLE_NOT_FOUND}");
        }

        // 2. Security Checks
        if (role.IsSystemRole)
        {
            _logger.LogWarning("Attempted to delete system role {RoleId}. Error code: {ErrorCode}", id,
                ErrorCodes.INVALID_OPERATION);
            throw new InvalidOperationException(
                $"Cannot delete system roles. Error code: {ErrorCodes.INVALID_OPERATION}");
        }

        if (!CanAccessRole(role))
        {
            throw new UnauthorizedAccessException(
                $"You don't have permission to delete this role. Error code: {ErrorCodes.FORBIDDEN}");
        }

        // 3. Usage Check (Prevent Orphaned Users)
        var userCount = await _roleRepository.GetRoleUserCountAsync(id);
        if (userCount > 0)
        {
            _logger.LogWarning("Cannot delete role {RoleId} with {UserCount} assigned users. Error code: {ErrorCode}",
                id, userCount, ErrorCodes.CONFLICT);
            throw new InvalidOperationException(
                $"Cannot delete role because it is assigned to {userCount} users. Please reassign them first. Error code: {ErrorCodes.CONFLICT}");
        }

        // 4. Perform Soft Delete
        var deleted = await _roleRepository.SoftDeleteRoleAsync(id);
        
        return deleted;
    }

// ---------------------------------------------------------
// HARD DELETE (Permanent Removal)
// ---------------------------------------------------------
    public async Task<bool> HardDeleteRoleAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty.", nameof(id));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} PERMANENTLY deleting role {RoleId}", userId, id);

        // 1. Get Role (Even if deleted)
        // Note: GetRoleByIdAsync in repo uses IgnoreQueryFilters so it finds deleted roles
        var role = await _roleRepository.GetRoleByIdAsync(id);

        if (role == null) throw new KeyNotFoundException($"Role {id} not found.");

        // 2. Only allow Hard Delete if already Soft Deleted (Safety)
        if (!role.IsDeleted)
        {
            throw new InvalidOperationException(
                "Role must be soft-deleted (moved to trash) before it can be permanently deleted.");
        }

        // 3. Security Checks
        if (role.IsSystemRole) throw new InvalidOperationException("Cannot delete system roles.");
        if (!CanAccessRole(role)) throw new UnauthorizedAccessException("Permission denied.");

        // 4. Perform Hard Delete
        return await _roleRepository.HardDeleteRoleAsync(id); 
    }

    /// <summary>
    /// Restore a soft-deleted role
    /// </summary>
    public async Task<RoleResponseDto?> RestoreRoleAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(id));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} restoring role {RoleId}", userId, id);

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found for restore. Error code: {ErrorCode}",
                id, ErrorCodes.ROLE_NOT_FOUND);
            return null;
        }

        if (!role.IsDeleted)
        {
            _logger.LogWarning("Role {RoleId} is not deleted. Error code: {ErrorCode}",
                id, ErrorCodes.INVALID_OPERATION);
            throw new InvalidOperationException($"Role is not deleted. Error code: {ErrorCodes.INVALID_OPERATION}");
        }

        if (!CanAccessRole(role))
        {
            _logger.LogWarning(
                "User {UserId} attempted to restore role {RoleId} without permission. Error code: {ErrorCode}",
                userId, id, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You don't have permission to restore this role. Error code: {ErrorCodes.FORBIDDEN}");
        }

        var restored = await _roleRepository.RestoreRoleAsync(id);

        if (restored == null)
        {
            _logger.LogWarning("Failed to restore role {RoleId}. Error code: {ErrorCode}", id,
                ErrorCodes.INTERNAL_ERROR);
            return null;
        }

        var response = _mapper.Map<RoleResponseDto>(restored);
        response.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

        _logger.LogInformation("User {UserId} restored role {RoleId}", userId, id);
        return response;
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(permissionId));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} assigning permission {PermissionId} to role {RoleId}",
            userId, permissionId, roleId);

        return await _roleRepository.AssignPermissionToRoleAsync(roleId, permissionId);
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(permissionId));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} removing permission {PermissionId} from role {RoleId}",
            userId, permissionId, roleId);

        return await _roleRepository.RemovePermissionFromRoleAsync(roleId, permissionId);
    }

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(roleId));

        var enumerable = permissionIds.ToList();
        if (permissionIds == null || enumerable.Count == 0)
            throw new ArgumentException($"Permission IDs cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(permissionIds));

        var userId = _userContext.GetCurrentUserId();
        _logger.LogInformation("User {UserId} bulk assigning {Count} permissions to role {RoleId}",
            userId, enumerable.Count(), roleId);

        return await _roleRepository.BulkAssignPermissionsAsync(roleId, enumerable);
    }

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}",
                nameof(roleId));

        if (permissionIds == null)
            throw new ArgumentNullException(nameof(permissionIds));

        var userId = _userContext.GetCurrentUserId();
        var enumerable = permissionIds.ToList();
        _logger.LogInformation("User {UserId} syncing {Count} permissions for role {RoleId}",
            userId, enumerable.Count, roleId);

        return await _roleRepository.SyncRolePermissionsAsync(roleId, enumerable);
    }

    // ============================================
    // HELPER METHODS
    // ============================================

    private async Task<IEnumerable<RoleResponseDto>> MapRolesToDtos(IEnumerable<Role> roles)
    {
        var dtos = new List<RoleResponseDto>();
        foreach (var role in roles)
        {
            var dto = _mapper.Map<RoleResponseDto>(role);
            dto.UserCount = await _roleRepository.GetRoleUserCountAsync(role.Id);
            dto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(role.Id);
            dtos.Add(dto);
        }

        return dtos;
    }

    private bool CanAccessRole(Role? role)
    {
        if (role == null)
            return false;

        if (_tenantContext.IsSuperAdmin)
            return true;

        if (role.IsSystemRole)
            return false;

        if (role.IsTemplate)
            return true;

        return role.ClinicId.HasValue && role.ClinicId == _tenantContext.ClinicId;
    }

    private async Task CopyPermissionsFromTemplate(Guid targetRoleId, Guid templateRoleId)
    {
        var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(templateRoleId);
        var enumerable = permissionIds.ToList();
        if (enumerable.Count != 0)
            await _roleRepository.BulkAssignPermissionsAsync(targetRoleId, enumerable);
    }

    private Task<Guid?> ValidateCreateRolePermissions(CreateRoleRequestDto dto)
    {
        if (_tenantContext.IsSuperAdmin)
            return Task.FromResult(dto.ClinicId);

        if (!_tenantContext.ClinicId.HasValue)
        {
            _logger.LogWarning("User {UserId} attempted to create role without clinic context. Error code: {ErrorCode}",
                _userContext.GetCurrentUserId(), ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You must belong to a clinic to create roles. Error code: {ErrorCodes.FORBIDDEN}");
        }

        if (dto.ClinicId.HasValue && dto.ClinicId != _tenantContext.ClinicId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to create role for clinic {ClinicId} without permission. Error code: {ErrorCode}",
                _userContext.GetCurrentUserId(), dto.ClinicId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You can only create roles for your own clinic. Error code: {ErrorCodes.FORBIDDEN}");
        }

        return Task.FromResult(_tenantContext.ClinicId);
    }

    private Task<Guid?> ValidateCloneRolePermissions(Guid? targetClinicId)
    {
        if (_tenantContext.IsSuperAdmin)
        {
            // if (!targetClinicId.HasValue)
            // {
            //     _logger.LogWarning("SuperAdmin {UserId} attempted to clone role without specifying target clinic. Error code: {ErrorCode}",
            //         _userContext.GetCurrentUserId(), ErrorCodes.INVALID_ARGUMENT);
            //     throw new InvalidOperationException($"SuperAdmin must specify target clinic. Error code: {ErrorCodes.INVALID_ARGUMENT}");
            // }
            return Task.FromResult(targetClinicId);
        }

        if (!_tenantContext.ClinicId.HasValue)
        {
            _logger.LogWarning("User {UserId} attempted to clone role without clinic context. Error code: {ErrorCode}",
                _userContext.GetCurrentUserId(), ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You must belong to a clinic to clone roles. Error code: {ErrorCodes.FORBIDDEN}");
        }

        if (targetClinicId.HasValue && targetClinicId != _tenantContext.ClinicId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to clone role to clinic {TargetClinicId} without permission. Error code: {ErrorCode}",
                _userContext.GetCurrentUserId(), targetClinicId, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException(
                $"You can only clone roles to your own clinic. Error code: {ErrorCodes.FORBIDDEN}");
        }

        return Task.FromResult(_tenantContext.ClinicId);
    }
}