using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
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

    public RoleService(
        IRoleRepository roleRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<RoleService> logger,
        IPermissionEvaluator permissionEvaluator)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    // =================================================================
    // QUERY METHODS
    // =================================================================

    public async Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync(RoleFilterRequestDto filter)
    {
        // 1. Security Scope
        if (!_tenantContext.IsSuperAdmin)
        {
            filter.ClinicId = _tenantContext.ClinicId;
            
            // 🔐 PERMISSION CHECK
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.ViewAll))
                throw new UnauthorizedAccessException("You lack permissions to view roles.");
        }

        // 2. ABAC Filter
        Func<IQueryable<Role>, IQueryable<Role>> abacFilter = query =>
            _permissionEvaluator.FilterRoles(query, Common.Constants.Permissions.Roles.ViewAll, _userContext);

        // 3. Call Unified Repository Method
        var roles = await _roleRepository.GetAllRolesAsync(filter, abacFilter);
        return await MapRolesToDtos(roles);
    }

    public async Task<PaginatedResult<RoleResponseDto>> GetAllPagedAsync(RoleFilterRequestDto filter)
    {
        // 1. Security Scope
        if (!_tenantContext.IsSuperAdmin)
        {
            filter.ClinicId = _tenantContext.ClinicId;

            // 🔐 PERMISSION CHECK
            if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.ViewAll))
                throw new UnauthorizedAccessException("You lack permissions to view roles.");
        }

        // 2. ABAC Filter
        Func<IQueryable<Role>, IQueryable<Role>> abacFilter = query =>
            _permissionEvaluator.FilterRoles(query, Common.Constants.Permissions.Roles.ViewAll, _userContext);

        // 3. Call Unified Repository Method
        var result = await _roleRepository.GetAllPagedAsync(filter, abacFilter);
        
        var items = await MapRolesToDtos(result.Items);

        return new PaginatedResult<RoleResponseDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<IEnumerable<RoleResponseDto>> GetSystemRolesAsync()
    {
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only SuperAdmin can view system roles.");
            
        // 🔐 PERMISSION CHECK (SuperAdmin usually has all, but good to be explicit)
        if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.ViewSystem))
             throw new UnauthorizedAccessException("You lack permissions to view system roles.");

        var roles = await _roleRepository.GetSystemRolesAsync();
        return await MapRolesToDtos(roles);
    }

    public async Task<IEnumerable<RoleResponseDto>> GetTemplateRolesAsync()
    {
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.ViewTemplates))
             throw new UnauthorizedAccessException("You lack permissions to view template roles.");

        var roles = await _roleRepository.GetTemplateRolesAsync();
        return await MapRolesToDtos(roles);
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return null;

        // 1. Scope Check
        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("You don't have permission to view this role (Scope).");

        // 🔐 2. PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.View))
             throw new UnauthorizedAccessException("You lack permissions to view role details.");

        var dto = _mapper.Map<RoleResponseDto>(role);
        dto.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        dto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

        return dto;
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(Guid roleId)
    {
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));

        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);

        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("You don't have permission to view this role's permissions.");
            
        // 🔐 PERMISSION CHECK (Uses 'View' or specific 'ViewPermissions' if you had one)
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.View))
             throw new UnauthorizedAccessException("You lack permissions to view role permissions.");

        var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(roleId);
        return permissionIds.Select(p => p.ToString());
    }

    // ============================================
    // COMMAND METHODS
    // ============================================

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Create))
             throw new UnauthorizedAccessException("You lack permissions to create roles.");
        
        // 1. Validate Permissions & Scope
        var targetClinicId = await ValidateCreateRolePermissions(dto);

        // 2. Validate Uniqueness
        if (await _roleRepository.RoleNameExistsAsync(dto.Name, targetClinicId))
            throw new DomainException($"Role '{dto.Name}' already exists.", ErrorCodes.DUPLICATE_NAME, "Name");

        var role = _mapper.Map<Role>(dto);
        role.Id = Guid.NewGuid();
        role.ClinicId = targetClinicId;
        role.CreatedAt = DateTime.UtcNow;
        role.CreatedBy = _userContext.GetCurrentUserId();

        var created = await _roleRepository.CreateRoleAsync(role);

        if (dto.TemplateRoleId.HasValue)
            await CopyPermissionsFromTemplate(created.Id, dto.TemplateRoleId.Value);

        var response = _mapper.Map<RoleResponseDto>(created);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);

        _logger.LogInformation("Role {RoleId} created", created.Id);
        return response;
    }

    public async Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, CloneRoleRequestDto dto)
    {
        if (templateRoleId == Guid.Empty) throw new ArgumentException("Template ID required", nameof(templateRoleId));

        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Clone))
             throw new UnauthorizedAccessException("You lack permissions to clone roles.");

        var templateRole = await _roleRepository.GetRoleByIdAsync(templateRoleId);
        if (templateRole == null)
            throw new DomainException($"Template role {templateRoleId} not found.", ErrorCodes.ROLE_NOT_FOUND);

        var targetClinicId = await ValidateCloneRolePermissions(dto.ClinicId);

        // Logic for System/Template flags
        bool isSystemRole = false;
        bool isTemplate = false;
        if (targetClinicId == null) // SuperAdmin creating global role
        {
            if (templateRole.IsSystemRole) isSystemRole = true;
            else isTemplate = true;
        }

        var roleName = string.IsNullOrWhiteSpace(dto.NewRoleName)
            ? $"{templateRole.Name} (Copy)"
            : dto.NewRoleName.Trim();

        if (await _roleRepository.RoleNameExistsAsync(roleName, targetClinicId))
            throw new DomainException($"Role '{roleName}' already exists.", ErrorCodes.DUPLICATE_NAME, "NewRoleName");

        var clonedRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            ClinicId = targetClinicId,
            Description = !string.IsNullOrWhiteSpace(dto.Description) ? dto.Description : templateRole.Description,
            ColorCode = !string.IsNullOrWhiteSpace(dto.ColorCode) ? dto.ColorCode : templateRole.ColorCode,
            IconClass = !string.IsNullOrWhiteSpace(dto.IconClass) ? dto.IconClass : templateRole.IconClass,
            DisplayOrder = dto.DisplayOrder ?? templateRole.DisplayOrder,
            IsSystemRole = isSystemRole,
            IsTemplate = isTemplate,
            TemplateRoleId = templateRoleId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userContext.GetCurrentUserId()
        };

        var created = await _roleRepository.CreateRoleAsync(clonedRole);

        if (dto.CopyPermissions)
            await CopyPermissionsFromTemplate(created.Id, templateRoleId);

        var response = _mapper.Map<RoleResponseDto>(created);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);

        return response;
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return null; // 404

        if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
            throw new DomainException("Cannot update system roles.", ErrorCodes.INVALID_OPERATION);

        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("You don't have permission to update this role (Scope).");
            
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Edit))
             throw new UnauthorizedAccessException("You lack permissions to edit roles.");

        // Check Name Uniqueness if changed
        if (!string.Equals(role.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
        {
             if (await _roleRepository.RoleNameExistsAsync(dto.Name, role.ClinicId))
                throw new DomainException($"Role '{dto.Name}' already exists.", ErrorCodes.DUPLICATE_NAME, "Name");
        }

        _mapper.Map(dto, role);
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = _userContext.GetCurrentUserId();

        var updated = await _roleRepository.UpdateRoleAsync(role);
        
        var response = _mapper.Map<RoleResponseDto>(updated);
        response.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

        return response;
    }

    public async Task<bool> SoftDeleteRoleAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return false;

        if (role.IsSystemRole)
            throw new DomainException("Cannot delete system roles.", ErrorCodes.INVALID_OPERATION);

        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("Permission denied (Scope).");
            
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Delete))
             throw new UnauthorizedAccessException("You lack permissions to delete roles.");

        var userCount = await _roleRepository.GetRoleUserCountAsync(id);
        if (userCount > 0)
            throw new DomainException($"Cannot delete role with {userCount} assigned users. Reassign them first.", ErrorCodes.CONFLICT);

        return await _roleRepository.SoftDeleteRoleAsync(id);
    }

    public async Task<bool> HardDeleteRoleAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        
        // 🔐 PERMISSION CHECK (Strict)
        if (!_tenantContext.IsSuperAdmin)
        {
             if (!await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.HardDelete))
                 throw new UnauthorizedAccessException("Only SuperAdmin or authorized users can permanently delete roles.");
        }

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return false;

        if (!role.IsDeleted)
            throw new DomainException("Role must be soft-deleted first.", ErrorCodes.INVALID_OPERATION);

        if (role.IsSystemRole) throw new DomainException("Cannot delete system roles.", ErrorCodes.INVALID_OPERATION);
        if (!CanAccessRole(role)) throw new UnauthorizedAccessException("Permission denied.");

        return await _roleRepository.HardDeleteRoleAsync(id); 
    }

    public async Task<RoleResponseDto?> RestoreRoleAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return null;

        if (!role.IsDeleted)
            throw new DomainException("Role is not deleted.", ErrorCodes.INVALID_OPERATION);

        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("Permission denied (Scope).");
            
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Restore))
             throw new UnauthorizedAccessException("You lack permissions to restore roles.");

        var restored = await _roleRepository.RestoreRoleAsync(id);
        if (restored == null) return null;

        var response = _mapper.Map<RoleResponseDto>(restored);
        response.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

        return response;
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty || permissionId == Guid.Empty) throw new ArgumentException("Invalid IDs");
        
        // 🔐 PERMISSION CHECK (Usually guarded by 'AppPermissions.Assign' in Endpoint, but good to check access to Role)
        // Here we just check if they can EDIT the role essentially
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Edit))
             throw new UnauthorizedAccessException("You lack permissions to modify role permissions.");
        
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);
        if (!CanAccessRole(role)) throw new UnauthorizedAccessException("Access denied.");

        return await _roleRepository.AssignPermissionToRoleAsync(roleId, permissionId);
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty || permissionId == Guid.Empty) throw new ArgumentException("Invalid IDs");
        
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Edit))
             throw new UnauthorizedAccessException("You lack permissions to modify role permissions.");
        
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);
        if (!CanAccessRole(role)) throw new UnauthorizedAccessException("Access denied.");

        return await _roleRepository.RemovePermissionFromRoleAsync(roleId, permissionId);
    }

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Edit))
             throw new UnauthorizedAccessException("You lack permissions to modify role permissions.");

        var list = permissionIds.ToList();
        if (!list.Any()) return 0;
        return await _roleRepository.BulkAssignPermissionsAsync(roleId, list);
    }

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        if (permissionIds == null) throw new ArgumentNullException(nameof(permissionIds));
        
        // 🔐 PERMISSION CHECK
        if (!_tenantContext.IsSuperAdmin && !await _permissionEvaluator.HasAsync(Common.Constants.Permissions.Roles.Edit))
             throw new UnauthorizedAccessException("You lack permissions to modify role permissions.");
        
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null) throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);
        if (!CanAccessRole(role)) throw new UnauthorizedAccessException("Access denied.");

        return await _roleRepository.SyncRolePermissionsAsync(roleId, permissionIds.ToList());
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
        if (role == null) return false;
        if (_tenantContext.IsSuperAdmin) return true;
        if (role.IsSystemRole) return false;
        if (role.IsTemplate) return true;
        return role.ClinicId.HasValue && role.ClinicId == _tenantContext.ClinicId;
    }

    private async Task CopyPermissionsFromTemplate(Guid targetRoleId, Guid templateRoleId)
    {
        var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(templateRoleId);
        var list = permissionIds.ToList();
        if (list.Any()) await _roleRepository.BulkAssignPermissionsAsync(targetRoleId, list);
    }

    private Task<Guid?> ValidateCreateRolePermissions(CreateRoleRequestDto dto)
    {
        if (_tenantContext.IsSuperAdmin) return Task.FromResult(dto.ClinicId);

        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to create roles.");

        if (dto.ClinicId.HasValue && dto.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only create roles for your own clinic.");

        return Task.FromResult(_tenantContext.ClinicId);
    }

    private Task<Guid?> ValidateCloneRolePermissions(Guid? targetClinicId)
    {
        if (_tenantContext.IsSuperAdmin) return Task.FromResult(targetClinicId);

        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to clone roles.");

        if (targetClinicId.HasValue && targetClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only clone roles to your own clinic.");

        return Task.FromResult(_tenantContext.ClinicId);
    }
}