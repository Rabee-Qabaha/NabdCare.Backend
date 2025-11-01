using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Roles;

/// <summary>
/// Production-ready service for managing roles in the multi-tenant system.
/// Implements ABAC-aware pagination for secure, filtered role visibility.
/// </summary>
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


    #region PAGINATED QUERY METHODS

    public async Task<PaginatedResult<RoleResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested paginated roles (Limit={Limit}, Cursor={Cursor})", userId, pagination.Limit, pagination.Cursor);

            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to access all paginated roles", userId);
                throw new UnauthorizedAccessException("Only SuperAdmin can view all roles");
            }

            // ✅ ABAC integration — filter roles visible to the current user
            var result = await _roleRepository.GetAllPagedAsync(pagination, query =>
                _permissionEvaluator.FilterRoles(query, "Roles.View", _userContext));

            var items = await MapRolesToDtos(result.Items);

            return new PaginatedResult<RoleResponseDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                HasMore = result.HasMore,
                NextCursor = result.NextCursor
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated roles for user {UserId}", _userContext.GetCurrentUserId());
            throw new InvalidOperationException("Failed to retrieve roles. Please try again.", ex);
        }
    }

    public async Task<PaginatedResult<RoleResponseDto>> GetClinicRolesPagedAsync(Guid clinicId, PaginationRequestDto pagination)
    {
        try
        {
            if (clinicId == Guid.Empty)
                throw new ArgumentException("Clinic ID cannot be empty", nameof(clinicId));

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} requested paginated roles for clinic {ClinicId}", userId, clinicId);

            if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
            {
                _logger.LogWarning("User {UserId} attempted to view roles for clinic {ClinicId} without permission", userId, clinicId);
                throw new UnauthorizedAccessException("You can only view roles for your own clinic");
            }

            // ✅ ABAC filter applied here too
            var result = await _roleRepository.GetClinicRolesPagedAsync(clinicId, pagination, query =>
                _permissionEvaluator.FilterRoles(query, "Roles.View", _userContext));

            var items = await MapRolesToDtos(result.Items);

            return new PaginatedResult<RoleResponseDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                HasMore = result.HasMore,
                NextCursor = result.NextCursor
            };
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated roles for clinic {ClinicId}", clinicId);
            throw new InvalidOperationException($"Failed to retrieve roles for clinic {clinicId}. Please try again.", ex);
        }
    }

    #endregion

    #region QUERY METHODS

    public async Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("Getting all roles for user {UserId}", userId);

            IEnumerable<Role> roles;

            if (_tenantContext.IsSuperAdmin)
            {
                roles = (await _roleRepository.GetAllPagedAsync(new PaginationRequestDto { Limit = 100 })).Items;
                var list = roles.ToList();
                _logger.LogInformation("SuperAdmin {UserId} retrieved {Count} roles", userId, list.Count);
                return await MapRolesToDtos(list);
            }

            if (_tenantContext.ClinicId.HasValue)
            {
                var templates = await _roleRepository.GetTemplateRolesAsync();
                var clinicRoles = (await _roleRepository.GetClinicRolesPagedAsync(_tenantContext.ClinicId.Value, new PaginationRequestDto { Limit = 100 })).Items;
                roles = templates.Concat(clinicRoles).DistinctBy(r => r.Id);
                var list = roles.ToList();
                _logger.LogInformation("Clinic user {UserId} retrieved {Count} roles", userId, list.Count);
                return await MapRolesToDtos(list);
            }

            return Enumerable.Empty<RoleResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles for user {UserId}", _userContext.GetCurrentUserId());
            throw new InvalidOperationException("Failed to retrieve roles. Please try again.", ex);
        }
    }

    public async Task<IEnumerable<RoleResponseDto>> GetSystemRolesAsync()
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only SuperAdmin can view system roles");

            var roles = await _roleRepository.GetSystemRolesAsync();
            return await MapRolesToDtos(roles);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system roles");
            throw new InvalidOperationException("Failed to retrieve system roles. Please try again.", ex);
        }
    }

    public async Task<IEnumerable<RoleResponseDto>> GetTemplateRolesAsync()
    {
        try
        {
            var roles = await _roleRepository.GetTemplateRolesAsync();
            return await MapRolesToDtos(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template roles");
            throw new InvalidOperationException("Failed to retrieve template roles. Please try again.", ex);
        }
    }

    public async Task<IEnumerable<RoleResponseDto>> GetClinicRolesAsync(Guid clinicId)
    {
        try
        {
            if (clinicId == Guid.Empty)
                throw new ArgumentException("Clinic ID cannot be empty", nameof(clinicId));

            if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
                throw new UnauthorizedAccessException("You can only view roles for your own clinic");

            var roles = (await _roleRepository.GetClinicRolesPagedAsync(clinicId, new PaginationRequestDto { Limit = 100 })).Items;
            return await MapRolesToDtos(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for clinic {ClinicId}", clinicId);
            throw new InvalidOperationException($"Failed to retrieve roles for clinic {clinicId}. Please try again.", ex);
        }
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(Guid id)
    {
        try
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null) return null;

            if (!CanAccessRole(role))
                throw new UnauthorizedAccessException("You don't have permission to view this role");

            var dto = _mapper.Map<RoleResponseDto>(role);
            dto.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
            dto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", id);
            throw new InvalidOperationException($"Failed to retrieve role {id}. Please try again.", ex);
        }
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(Guid roleId)
    {
        try
        {
            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role {roleId} not found");

            if (!CanAccessRole(role))
                throw new UnauthorizedAccessException("You don't have permission to view this role's permissions");

            var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(roleId);
            return permissionIds.Select(p => p.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for role {RoleId}", roleId);
            throw new InvalidOperationException($"Failed to retrieve permissions for role {roleId}. Please try again.", ex);
        }
    }

    #endregion

    #region COMMAND METHODS

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Role name is required", nameof(dto.Name));

        var userId = _userContext.GetCurrentUserId();
        var targetClinicId = await ValidateCreateRolePermissions(dto);

        if (await _roleRepository.RoleNameExistsAsync(dto.Name, targetClinicId))
            throw new InvalidOperationException($"Role name '{dto.Name}' already exists");

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
        return response;
    }

    public async Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, Guid? targetClinicId, string? newRoleName)
    {
        var templateRole = await _roleRepository.GetRoleByIdAsync(templateRoleId)
            ?? throw new KeyNotFoundException($"Template role {templateRoleId} not found");

        if (!templateRole.IsTemplate)
            throw new InvalidOperationException("Can only clone template roles");

        var actualTargetClinicId = await ValidateCloneRolePermissions(targetClinicId);
        var roleName = string.IsNullOrWhiteSpace(newRoleName) ? templateRole.Name : newRoleName.Trim();

        if (await _roleRepository.RoleNameExistsAsync(roleName, actualTargetClinicId))
            throw new InvalidOperationException($"Role '{roleName}' already exists");

        var cloned = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            ClinicId = actualTargetClinicId,
            Description = templateRole.Description,
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userContext.GetCurrentUserId()
        };

        var created = await _roleRepository.CreateRoleAsync(cloned);
        await CopyPermissionsFromTemplate(created.Id, templateRoleId);

        var response = _mapper.Map<RoleResponseDto>(created);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);
        return response;
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto)
    {
        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return null;

        if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Cannot update system roles");

        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("You don't have permission to update this role");

        _mapper.Map(dto, role);
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = _userContext.GetCurrentUserId();

        var updated = await _roleRepository.UpdateRoleAsync(role);
        if (updated == null) return null;

        var response = _mapper.Map<RoleResponseDto>(updated);
        response.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
        response.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);
        return response;
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await _roleRepository.GetRoleByIdAsync(id);
        if (role == null) return false;

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot delete system roles");

        if (!CanAccessRole(role))
            throw new UnauthorizedAccessException("You don't have permission to delete this role");

        if (await _roleRepository.GetRoleUserCountAsync(id) > 0)
            throw new InvalidOperationException("Cannot delete role with assigned users");

        return await _roleRepository.DeleteRoleAsync(id);
    }

    #endregion

    #region PERMISSION MANAGEMENT

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
        => await _roleRepository.AssignPermissionToRoleAsync(roleId, permissionId);

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
        => await _roleRepository.RemovePermissionFromRoleAsync(roleId, permissionId);

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        => await _roleRepository.BulkAssignPermissionsAsync(roleId, permissionIds);

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        => await _roleRepository.SyncRolePermissionsAsync(roleId, permissionIds);

    #endregion

    #region HELPER METHODS

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
        if (permissionIds.Any())
            await _roleRepository.BulkAssignPermissionsAsync(targetRoleId, permissionIds);
    }

    private async Task<Guid?> ValidateCreateRolePermissions(CreateRoleRequestDto dto)
    {
        if (_tenantContext.IsSuperAdmin) return dto.ClinicId;

        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to create roles");

        if (dto.ClinicId.HasValue && dto.ClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only create roles for your own clinic");

        return _tenantContext.ClinicId;
    }

    private async Task<Guid?> ValidateCloneRolePermissions(Guid? targetClinicId)
    {
        if (_tenantContext.IsSuperAdmin)
            return targetClinicId ?? throw new InvalidOperationException("SuperAdmin must specify target clinic");

        if (!_tenantContext.ClinicId.HasValue)
            throw new UnauthorizedAccessException("You must belong to a clinic to clone roles");

        if (targetClinicId.HasValue && targetClinicId != _tenantContext.ClinicId)
            throw new UnauthorizedAccessException("You can only clone roles to your own clinic");

        return _tenantContext.ClinicId;
    }

    #endregion
}