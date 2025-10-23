using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Roles;

/// <summary>
/// Production-ready service for managing roles in the multi-tenant system.
/// Implements comprehensive error handling, validation, and logging.
/// Zero compiler warnings. Optimized for performance.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        IRoleRepository roleRepository,
        ITenantContext tenantContext,
        IUserContext userContext,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
                roles = await _roleRepository.GetAllRolesAsync();
                var rolesList = roles.ToList(); // ✅ FIX: Materialize once to avoid multiple enumeration
                _logger.LogInformation("SuperAdmin {UserId} retrieved {Count} roles", userId, rolesList.Count);
                return await MapRolesToDtos(rolesList);
            }
            
            if (_tenantContext.ClinicId.HasValue)
            {
                var templates = await _roleRepository.GetTemplateRolesAsync();
                var clinicRoles = await _roleRepository.GetClinicRolesAsync(_tenantContext.ClinicId.Value);
                roles = templates.Concat(clinicRoles).DistinctBy(r => r.Id);
                var rolesList = roles.ToList(); // ✅ FIX: Materialize once
                _logger.LogInformation("Clinic user {UserId} retrieved {Count} roles for clinic {ClinicId}", 
                    userId, rolesList.Count, _tenantContext.ClinicId.Value);
                return await MapRolesToDtos(rolesList);
            }
            
            _logger.LogWarning("User {UserId} has no tenant context, returning empty role list", userId);
            return Enumerable.Empty<RoleResponseDto>();
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Error retrieving all roles for user {UserId}", _userContext.GetCurrentUserId());
            throw new InvalidOperationException("Failed to retrieve roles. Please try again.", ex);
        }
    }

    public async Task<IEnumerable<RoleResponseDto>> GetSystemRolesAsync()
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to access system roles", userId);
                throw new UnauthorizedAccessException("Only SuperAdmin can view system roles");
            }

            _logger.LogInformation("SuperAdmin {UserId} retrieving system roles", userId);
            var roles = await _roleRepository.GetSystemRolesAsync();
            var rolesList = roles.ToList(); // ✅ FIX: Materialize once
            
            _logger.LogInformation("SuperAdmin {UserId} retrieved {Count} system roles", userId, rolesList.Count);
            return await MapRolesToDtos(rolesList);
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system roles for user {UserId}", _userContext.GetCurrentUserId());
            throw new InvalidOperationException("Failed to retrieve system roles. Please try again.", ex);
        }
    }

    public async Task<IEnumerable<RoleResponseDto>> GetTemplateRolesAsync()
    {
        try
        {
            _logger.LogInformation("User {UserId} retrieving template roles", _userContext.GetCurrentUserId());
            
            var roles = await _roleRepository.GetTemplateRolesAsync();
            var rolesList = roles.ToList(); // ✅ FIX: Materialize once
            
            _logger.LogInformation("Retrieved {Count} template roles", rolesList.Count);
            return await MapRolesToDtos(rolesList);
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
            // Input validation
            if (clinicId == Guid.Empty)
            {
                throw new ArgumentException("Clinic ID cannot be empty", nameof(clinicId));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} retrieving roles for clinic {ClinicId}", userId, clinicId);

            // Authorization check
            if (!_tenantContext.IsSuperAdmin && _tenantContext.ClinicId != clinicId)
            {
                _logger.LogWarning("User {UserId} attempted to access roles for clinic {ClinicId} without permission", 
                    userId, clinicId);
                throw new UnauthorizedAccessException("You can only view roles for your own clinic");
            }

            var roles = await _roleRepository.GetClinicRolesAsync(clinicId);
            var rolesList = roles.ToList(); // ✅ FIX: Materialize once
            
            _logger.LogInformation("Retrieved {Count} roles for clinic {ClinicId}", rolesList.Count, clinicId);
            return await MapRolesToDtos(rolesList);
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
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
            // Input validation
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(id));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} retrieving role {RoleId}", userId, id);

            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", id);
                return null;
            }

            // Authorization check
            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to access role {RoleId} without permission", userId, id);
                throw new UnauthorizedAccessException("You don't have permission to view this role");
            }

            var dto = _mapper.Map<RoleResponseDto>(role);
            dto.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
            dto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

            _logger.LogInformation("User {UserId} successfully retrieved role {RoleId}", userId, id);
            return dto;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
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
            // Input validation
            if (roleId == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} retrieving permissions for role {RoleId}", userId, roleId);

            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                throw new KeyNotFoundException($"Role {roleId} not found");
            }

            // Authorization check
            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to access permissions for role {RoleId} without permission", 
                    userId, roleId);
                throw new UnauthorizedAccessException("You don't have permission to view this role's permissions");
            }

            var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(roleId);
            var permissionIdsList = permissionIds.ToList(); // ✅ FIX: Materialize once
            
            _logger.LogInformation("Retrieved {Count} permissions for role {RoleId}", permissionIdsList.Count, roleId);
            return permissionIdsList.Select(p => p.ToString());
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
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
        try
        {
            // Input validation
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Role name is required", nameof(dto.Name));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} creating role {RoleName}", userId, dto.Name);

            // Determine target clinic and validate permissions
            var targetClinicId = await ValidateCreateRolePermissions(dto);

            // Check for duplicate name
            if (await _roleRepository.RoleNameExistsAsync(dto.Name, targetClinicId))
            {
                _logger.LogWarning("Role with name '{RoleName}' already exists in context {ClinicId}", 
                    dto.Name, targetClinicId);
                throw new InvalidOperationException($"A role with name '{dto.Name}' already exists in this context");
            }

            // Map and create
            var role = _mapper.Map<Role>(dto);
            role.Id = Guid.NewGuid();
            role.ClinicId = targetClinicId;
            role.IsSystemRole = false;
            role.CreatedAt = DateTime.UtcNow;
            role.CreatedBy = userId;
            role.IsDeleted = false;

            var created = await _roleRepository.CreateRoleAsync(role);

            // Copy permissions from template if specified
            if (dto.TemplateRoleId.HasValue)
            {
                await CopyPermissionsFromTemplate(created.Id, dto.TemplateRoleId.Value);
            }

            _logger.LogInformation("User {UserId} successfully created role {RoleId} ({RoleName})", 
                userId, created.Id, created.Name);

            var responseDto = _mapper.Map<RoleResponseDto>(created);
            responseDto.UserCount = 0;
            responseDto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);

            return responseDto;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role {RoleName} for user {UserId}", 
                dto.Name, _userContext.GetCurrentUserId());
            throw new InvalidOperationException("Failed to create role. Please try again.", ex);
        }
    }

    public async Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, Guid? targetClinicId, string? newRoleName)
    {
        try
        {
            // Input validation
            if (templateRoleId == Guid.Empty)
            {
                throw new ArgumentException("Template role ID cannot be empty", nameof(templateRoleId));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} cloning role {TemplateRoleId} to clinic {ClinicId}", 
                userId, templateRoleId, targetClinicId);

            // Get and validate template role
            var templateRole = await _roleRepository.GetRoleByIdAsync(templateRoleId);
            if (templateRole == null)
            {
                _logger.LogWarning("Template role {TemplateRoleId} not found", templateRoleId);
                throw new KeyNotFoundException($"Template role {templateRoleId} not found");
            }

            if (!templateRole.IsTemplate)
            {
                _logger.LogWarning("Role {RoleId} is not a template role", templateRoleId);
                throw new InvalidOperationException("Can only clone template roles");
            }

            // Determine actual target clinic and validate permissions
            var actualTargetClinicId = await ValidateCloneRolePermissions(targetClinicId);

            // Generate role name
            var roleName = string.IsNullOrWhiteSpace(newRoleName) ? templateRole.Name : newRoleName.Trim();

            // Check for duplicate
            if (await _roleRepository.RoleNameExistsAsync(roleName, actualTargetClinicId))
            {
                _logger.LogWarning("Role with name '{RoleName}' already exists in clinic {ClinicId}", 
                    roleName, actualTargetClinicId);
                throw new InvalidOperationException($"A role with name '{roleName}' already exists in this clinic");
            }

            // Clone role
            var clonedRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                Description = templateRole.Description,
                ClinicId = actualTargetClinicId,
                IsSystemRole = false,
                IsTemplate = false,
                TemplateRoleId = templateRoleId,
                DisplayOrder = templateRole.DisplayOrder,
                ColorCode = templateRole.ColorCode,
                IconClass = templateRole.IconClass,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                IsDeleted = false
            };

            var created = await _roleRepository.CreateRoleAsync(clonedRole);

            // Copy permissions from template
            await CopyPermissionsFromTemplate(created.Id, templateRoleId);

            _logger.LogInformation("User {UserId} successfully cloned role {TemplateRoleId} to {NewRoleId} for clinic {ClinicId}", 
                userId, templateRoleId, created.Id, actualTargetClinicId);

            var responseDto = _mapper.Map<RoleResponseDto>(created);
            responseDto.UserCount = 0;
            responseDto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(created.Id);

            return responseDto;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning role {TemplateRoleId} for user {UserId}", 
                templateRoleId, _userContext.GetCurrentUserId());
            throw new InvalidOperationException("Failed to clone role. Please try again.", ex);
        }
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto)
    {
        try
        {
            // Input validation
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(id));
            }

            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Role name is required", nameof(dto.Name));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} updating role {RoleId}", userId, id);

            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", id);
                return null;
            }

            // Business rule validation
            if (role.IsSystemRole)
            {
                _logger.LogWarning("User {UserId} attempted to update system role {RoleId}", userId, id);
                throw new InvalidOperationException("Cannot update system roles");
            }

            // Authorization check
            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to update role {RoleId} without permission", userId, id);
                throw new UnauthorizedAccessException("You don't have permission to update this role");
            }

            // Check for duplicate name (excluding current role)
            if (await _roleRepository.RoleNameExistsAsync(dto.Name, role.ClinicId, id))
            {
                _logger.LogWarning("Role with name '{RoleName}' already exists", dto.Name);
                throw new InvalidOperationException($"A role with name '{dto.Name}' already exists");
            }

            // Only SuperAdmin can change IsTemplate
            if (!_tenantContext.IsSuperAdmin && dto.IsTemplate != role.IsTemplate)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to change template status for role {RoleId}", 
                    userId, id);
                throw new UnauthorizedAccessException("Only SuperAdmin can change template status");
            }

            // Update fields
            role.Name = dto.Name.Trim();
            role.Description = dto.Description?.Trim();
            role.DisplayOrder = dto.DisplayOrder;
            role.ColorCode = dto.ColorCode?.Trim();
            role.IconClass = dto.IconClass?.Trim();
            role.IsTemplate = dto.IsTemplate;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = userId;

            var updated = await _roleRepository.UpdateRoleAsync(role);

            _logger.LogInformation("User {UserId} successfully updated role {RoleId}", userId, id);

            // ✅ FIX: Handle nullable properly
            if (updated == null)
            {
                _logger.LogWarning("Role {RoleId} disappeared after update", id);
                return null;
            }

            var responseDto = _mapper.Map<RoleResponseDto>(updated);
            responseDto.UserCount = await _roleRepository.GetRoleUserCountAsync(id);
            responseDto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(id);

            return responseDto;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId} for user {UserId}", 
                id, _userContext.GetCurrentUserId());
            throw new InvalidOperationException($"Failed to update role {id}. Please try again.", ex);
        }
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        try
        {
            // Input validation
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(id));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} deleting role {RoleId}", userId, id);

            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", id);
                return false;
            }

            // Business rule validations
            if (role.IsSystemRole)
            {
                _logger.LogWarning("User {UserId} attempted to delete system role {RoleId}", userId, id);
                throw new InvalidOperationException("Cannot delete system roles");
            }

            // Authorization check
            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to delete role {RoleId} without permission", userId, id);
                throw new UnauthorizedAccessException("You don't have permission to delete this role");
            }

            // Check for assigned users
            var userCount = await _roleRepository.GetRoleUserCountAsync(id);
            if (userCount > 0)
            {
                _logger.LogWarning("Cannot delete role {RoleId} with {UserCount} assigned users", id, userCount);
                throw new InvalidOperationException(
                    $"Cannot delete role with {userCount} assigned user(s). Reassign users first.");
            }

            // Check template status
            if (role.IsTemplate)
            {
                _logger.LogWarning("Cannot delete template role {RoleId}", id);
                throw new InvalidOperationException(
                    "Cannot delete template roles. Set IsTemplate to false first.");
            }

            var deleted = await _roleRepository.DeleteRoleAsync(id);

            if (deleted)
            {
                _logger.LogInformation("User {UserId} successfully deleted role {RoleId}", userId, id);
            }
            else
            {
                _logger.LogWarning("Failed to delete role {RoleId}", id);
            }

            return deleted;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw business logic exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId} for user {UserId}", 
                id, _userContext.GetCurrentUserId());
            throw new InvalidOperationException($"Failed to delete role {id}. Please try again.", ex);
        }
    }

    #endregion

    #region PERMISSION MANAGEMENT

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            // Input validation
            if (roleId == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
            }

            if (permissionId == Guid.Empty)
            {
                throw new ArgumentException("Permission ID cannot be empty", nameof(permissionId));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} assigning permission {PermissionId} to role {RoleId}", 
                userId, permissionId, roleId);

            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                throw new KeyNotFoundException($"Role {roleId} not found");
            }

            // Authorization checks
            if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to modify system role {RoleId} permissions", 
                    userId, roleId);
                throw new UnauthorizedAccessException("Only SuperAdmin can modify system role permissions");
            }

            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to modify role {RoleId} without permission", userId, roleId);
                throw new UnauthorizedAccessException("You don't have permission to modify this role");
            }

            var assigned = await _roleRepository.AssignPermissionToRoleAsync(roleId, permissionId);

            if (assigned)
            {
                _logger.LogInformation("User {UserId} successfully assigned permission {PermissionId} to role {RoleId}", 
                    userId, permissionId, roleId);
            }
            else
            {
                _logger.LogInformation("Permission {PermissionId} already assigned to role {RoleId}", 
                    permissionId, roleId);
            }

            return assigned;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", 
                permissionId, roleId);
            throw new InvalidOperationException(
                $"Failed to assign permission {permissionId} to role {roleId}. Please try again.", ex);
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            // Input validation
            if (roleId == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
            }

            if (permissionId == Guid.Empty)
            {
                throw new ArgumentException("Permission ID cannot be empty", nameof(permissionId));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} removing permission {PermissionId} from role {RoleId}", 
                userId, permissionId, roleId);

            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                throw new KeyNotFoundException($"Role {roleId} not found");
            }

            // Authorization checks
            if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to modify system role {RoleId} permissions", 
                    userId, roleId);
                throw new UnauthorizedAccessException("Only SuperAdmin can modify system role permissions");
            }

            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to modify role {RoleId} without permission", userId, roleId);
                throw new UnauthorizedAccessException("You don't have permission to modify this role");
            }

            var removed = await _roleRepository.RemovePermissionFromRoleAsync(roleId, permissionId);

            if (removed)
            {
                _logger.LogInformation("User {UserId} successfully removed permission {PermissionId} from role {RoleId}", 
                    userId, permissionId, roleId);
            }
            else
            {
                _logger.LogWarning("Permission {PermissionId} not found in role {RoleId}", permissionId, roleId);
            }

            return removed;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", 
                permissionId, roleId);
            throw new InvalidOperationException(
                $"Failed to remove permission {permissionId} from role {roleId}. Please try again.", ex);
        }
    }

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        try
        {
            // Input validation
            if (roleId == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
            }

            if (permissionIds == null)
            {
                throw new ArgumentNullException(nameof(permissionIds));
            }

            var permissionIdsList = permissionIds.Where(p => p != Guid.Empty).ToList();
            if (permissionIdsList.Count == 0)
            {
                throw new ArgumentException("At least one valid permission ID is required", nameof(permissionIds));
            }

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} bulk assigning {Count} permissions to role {RoleId}", 
                userId, permissionIdsList.Count, roleId);

            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                throw new KeyNotFoundException($"Role {roleId} not found");
            }

            // Authorization checks
            if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to modify system role {RoleId} permissions", 
                    userId, roleId);
                throw new UnauthorizedAccessException("Only SuperAdmin can modify system role permissions");
            }

            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to modify role {RoleId} without permission", userId, roleId);
                throw new UnauthorizedAccessException("You don't have permission to modify this role");
            }

            var count = await _roleRepository.BulkAssignPermissionsAsync(roleId, permissionIdsList);

            _logger.LogInformation("User {UserId} successfully assigned {Count} permissions to role {RoleId}", 
                userId, count, roleId);

            return count;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning permissions to role {RoleId}", roleId);
            throw new InvalidOperationException(
                $"Failed to bulk assign permissions to role {roleId}. Please try again.", ex);
        }
    }

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        try
        {
            // Input validation
            if (roleId == Guid.Empty)
            {
                throw new ArgumentException("Role ID cannot be empty", nameof(roleId));
            }

            if (permissionIds == null)
            {
                throw new ArgumentNullException(nameof(permissionIds));
            }

            var permissionIdsList = permissionIds.Where(p => p != Guid.Empty).ToList();

            var userId = _userContext.GetCurrentUserId();
            _logger.LogInformation("User {UserId} syncing permissions for role {RoleId} ({Count} permissions)", 
                userId, roleId, permissionIdsList.Count);

            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found", roleId);
                throw new KeyNotFoundException($"Role {roleId} not found");
            }

            // Authorization checks
            if (role.IsSystemRole && !_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Non-SuperAdmin user {UserId} attempted to modify system role {RoleId} permissions", 
                    userId, roleId);
                throw new UnauthorizedAccessException("Only SuperAdmin can modify system role permissions");
            }

            if (!CanAccessRole(role))
            {
                _logger.LogWarning("User {UserId} attempted to modify role {RoleId} without permission", userId, roleId);
                throw new UnauthorizedAccessException("You don't have permission to modify this role");
            }

            var synced = await _roleRepository.SyncRolePermissionsAsync(roleId, permissionIdsList);

            _logger.LogInformation("User {UserId} successfully synced permissions for role {RoleId}", userId, roleId);

            return synced;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw validation exceptions
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw not found exceptions
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Re-throw authorization exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing permissions for role {RoleId}", roleId);
            throw new InvalidOperationException(
                $"Failed to sync permissions for role {roleId}. Please try again.", ex);
        }
    }

    #endregion

    #region HELPER METHODS

    private async Task<IEnumerable<RoleResponseDto>> MapRolesToDtos(IEnumerable<Role> roles)
    {
        var dtos = new List<RoleResponseDto>();
        var rolesList = roles.ToList(); // ✅ FIX: Materialize to avoid multiple enumeration

        foreach (var role in rolesList)
        {
            try
            {
                var dto = _mapper.Map<RoleResponseDto>(role);
                dto.UserCount = await _roleRepository.GetRoleUserCountAsync(role.Id);
                dto.PermissionCount = await _roleRepository.GetRolePermissionCountAsync(role.Id);
                dtos.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping role {RoleId} to DTO", role.Id);
                // Continue with other roles instead of failing completely
            }
        }

        return dtos;
    }

    private bool CanAccessRole(Role? role) // ✅ FIX: Nullable parameter to match actual usage
    {
        if (role == null)
        {
            return false;
        }

        // SuperAdmin can access all roles
        if (_tenantContext.IsSuperAdmin)
        {
            return true;
        }

        // System roles: only SuperAdmin
        if (role.IsSystemRole)
        {
            return false;
        }

        // Template roles: everyone can view
        if (role.IsTemplate)
        {
            return true;
        }

        // Clinic-specific roles: only if in same clinic
        return role.ClinicId.HasValue && role.ClinicId == _tenantContext.ClinicId;
    }

    private async Task CopyPermissionsFromTemplate(Guid targetRoleId, Guid templateRoleId)
    {
        try
        {
            var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(templateRoleId);
            var permissionIdsList = permissionIds.ToList(); // ✅ FIX: Materialize once
            
            if (permissionIdsList.Count > 0)
            {
                var count = await _roleRepository.BulkAssignPermissionsAsync(targetRoleId, permissionIdsList);
                _logger.LogInformation(
                    "Copied {Count} permissions from template {TemplateRoleId} to role {RoleId}", 
                    count, templateRoleId, targetRoleId);
            }
            else
            {
                _logger.LogInformation(
                    "No permissions to copy from template {TemplateRoleId} to role {RoleId}", 
                    templateRoleId, targetRoleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error copying permissions from template {TemplateRoleId} to role {RoleId}", 
                templateRoleId, targetRoleId);
            // Don't throw - role creation should succeed even if permission copy fails
            // Permissions can be added manually later
        }
    }

    private async Task<Guid?> ValidateCreateRolePermissions(CreateRoleRequestDto dto)
    {
        Guid? targetClinicId = dto.ClinicId;

        if (_tenantContext.IsSuperAdmin)
        {
            // SuperAdmin can create roles for any clinic or system-level
            if (dto.IsTemplate && dto.ClinicId.HasValue)
            {
                throw new InvalidOperationException("Template roles cannot belong to a specific clinic");
            }
        }
        else
        {
            // ClinicAdmin can only create roles for their clinic
            if (!_tenantContext.ClinicId.HasValue)
            {
                throw new UnauthorizedAccessException("You must belong to a clinic to create roles");
            }

            if (dto.ClinicId.HasValue && dto.ClinicId != _tenantContext.ClinicId)
            {
                throw new UnauthorizedAccessException("You can only create roles for your own clinic");
            }

            if (dto.IsTemplate)
            {
                throw new UnauthorizedAccessException("Only SuperAdmin can create template roles");
            }

            targetClinicId = _tenantContext.ClinicId.Value;
        }

        return await Task.FromResult(targetClinicId);
    }

    private async Task<Guid?> ValidateCloneRolePermissions(Guid? targetClinicId)
    {
        Guid? actualTargetClinicId = targetClinicId;

        if (_tenantContext.IsSuperAdmin)
        {
            // SuperAdmin must specify target clinic
            if (!targetClinicId.HasValue)
            {
                throw new InvalidOperationException("SuperAdmin must specify target clinic ID when cloning");
            }
        }
        else
        {
            // ClinicAdmin can only clone to their own clinic
            if (!_tenantContext.ClinicId.HasValue)
            {
                throw new UnauthorizedAccessException("You must belong to a clinic to clone roles");
            }

            if (targetClinicId.HasValue && targetClinicId != _tenantContext.ClinicId)
            {
                throw new UnauthorizedAccessException("You can only clone roles to your own clinic");
            }

            actualTargetClinicId = _tenantContext.ClinicId.Value;
        }

        return await Task.FromResult(actualTargetClinicId);
    }

    #endregion
}