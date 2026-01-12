using AutoMapper;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Common.Exceptions;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ITenantContext tenant,
        IMapper mapper,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null) throw new ArgumentNullException(nameof(pagination));

        var result = await _permissionRepository.GetAllPagedAsync(pagination);
        var mapped = _mapper.Map<IEnumerable<PermissionResponseDto>>(result.Items);

        return new PaginatedResult<PermissionResponseDto>
        {
            Items = mapped,
            TotalCount = result.TotalCount,
            HasMore = result.HasMore,
            NextCursor = result.NextCursor
        };
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
    {
        var permissions = await _permissionRepository.GetAllPermissionsAsync();

        // ---------------------------------------------------------
        // SECURITY FILTER: Hide System permissions from Non-SuperAdmins
        // ---------------------------------------------------------
        if (!_tenant.IsSuperAdmin)
        {
            var restrictedCategories = new[] { "System", "Settings", "AuditLogs" };
        
            permissions = permissions.Where(p => 
                    !restrictedCategories.Contains(p.Category) && 
                    !p.Name.StartsWith("Clinics.")
            );
        }

        return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
    }

    public async Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var permission = await _permissionRepository.GetPermissionByIdAsync(id);
        if (permission == null) return null;

        return _mapper.Map<PermissionResponseDto>(permission);
    }

    public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        
        // Basic validation (Should ideally be in FluentValidator)
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new DomainException("Permission name is required.", ErrorCodes.INVALID_ARGUMENT, "Name");

        // Optional: Check existence if repository supports it
        // if (await _permissionRepository.ExistsByNameAsync(dto.Name))
        //    throw new DomainException($"Permission '{dto.Name}' already exists.", ErrorCodes.DUPLICATE_NAME, "Name");

        var entity = _mapper.Map<AppPermission>(dto);
        var created = await _permissionRepository.CreatePermissionAsync(entity);

        _logger.LogInformation("Created permission {PermissionId} ({PermissionName})", created.Id, created.Name);

        return _mapper.Map<PermissionResponseDto>(created);
    }

    public async Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var entity = _mapper.Map<AppPermission>(dto);
        var updated = await _permissionRepository.UpdatePermissionAsync(id, entity);

        if (updated == null) return null;

        _logger.LogInformation("Updated permission {PermissionId}", id);
        return _mapper.Map<PermissionResponseDto>(updated);
    }

    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("ID required", nameof(id));

        var deleted = await _permissionRepository.DeletePermissionAsync(id);

        if (deleted)
            _logger.LogInformation("Deleted permission {PermissionId}", id);
        else
            _logger.LogWarning("Permission {PermissionId} not found for deletion.", id);

        return deleted;
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId)
    {
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));

        var permissions = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
        return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));
        if (permissionId == Guid.Empty) throw new ArgumentException("Permission ID required", nameof(permissionId));

        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
            throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);

        var result = await _permissionRepository.AssignPermissionToRoleAsync(roleId, permissionId);

        if (result)
            _logger.LogInformation("Assigned permission {PermissionId} to role {RoleId}", permissionId, roleId);

        return result;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));
        if (permissionId == Guid.Empty) throw new ArgumentException("Permission ID required", nameof(permissionId));

        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        if (role == null)
            throw new DomainException($"Role {roleId} not found.", ErrorCodes.ROLE_NOT_FOUND);

        var result = await _permissionRepository.RemovePermissionFromRoleAsync(roleId, permissionId);

        if (result)
            _logger.LogInformation("Removed permission {PermissionId} from role {RoleId}", permissionId, roleId);

        return result;
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID required", nameof(userId));

        var permissions = await _permissionRepository.GetPermissionsByUserAsync(userId);
        return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required", nameof(userId));
        if (permissionId == Guid.Empty) throw new ArgumentException("Permission ID is required", nameof(permissionId));
    
        // ---------------------------------------------------------
        // 1. VALIDATION: Ensure User and Permission Exist
        // ---------------------------------------------------------
        
        var userExists = await _userRepository.ExistsAsync(userId); 
        if (!userExists)
            throw new DomainException($"User with ID {userId} not found.", ErrorCodes.USER_NOT_FOUND);
    
        var permissionDef = await _permissionRepository.GetPermissionByIdAsync(permissionId);
        if (permissionDef == null)
            throw new DomainException($"Permission with ID {permissionId} does not exist.", ErrorCodes.NOT_FOUND);
    
        // ---------------------------------------------------------
        // 2. SECURITY CHECK
        // ---------------------------------------------------------
        if (!_tenant.IsSuperAdmin)
        {
            var restrictedPrefixes = new[] { "System", "Clinics", "Settings", "AuditLogs" };
            if (restrictedPrefixes.Any(p => permissionDef.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Security Alert: User {CurrentUserId} tried to assign {PermissionName}", _tenant.UserId, permissionDef.Name);
                throw new UnauthorizedAccessException("You are not authorized to assign System-level permissions.");
            }
        }
    
        // ---------------------------------------------------------
        // 3. REDUNDANCY CHECK (Role Inheritance)
        // ---------------------------------------------------------
        var userInfo = await GetUserForAuthorizationAsync(userId);
        if (userInfo.HasValue)
        {
            var rolePermissions = await _permissionRepository.GetPermissionsByRoleAsync(userInfo.Value.RoleId);
            if (rolePermissions.Any(p => p.Id == permissionId))
            {
                return false; // Already inherited
            }
        }
    
        // ---------------------------------------------------------
        // 4. EXECUTE
        // ---------------------------------------------------------
        _logger.LogInformation("Assigning permission {PermissionName} to user {UserId}", permissionDef.Name, userId);
        return await _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID required", nameof(userId));
        if (permissionId == Guid.Empty) throw new ArgumentException("Permission ID required", nameof(permissionId));

        var result = await _permissionRepository.RemovePermissionFromUserAsync(userId, permissionId);

        if (result)
            _logger.LogInformation("Removed permission {PermissionId} from user {UserId}", permissionId, userId);

        return result;
    }

    public async Task<bool> ClearUserPermissionsAsync(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Invalid User ID", nameof(userId));
    
        _logger.LogInformation("Clearing all custom permissions for user {UserId}", userId);
        return await _permissionRepository.ClearUserPermissionsAsync(userId);
    }
    
    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID required", nameof(userId));
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));

        var rolePerms = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
        var userPerms = await _permissionRepository.GetPermissionsByUserAsync(userId);

        var combined = rolePerms.Concat(userPerms)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        return _mapper.Map<IEnumerable<PermissionResponseDto>>(combined);
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID required", nameof(userId));
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));
        if (string.IsNullOrWhiteSpace(permissionName)) throw new ArgumentException("Permission name required", nameof(permissionName));

        var effective = await GetUserEffectivePermissionsAsync(userId, roleId);
        return effective.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID required", nameof(userId));

        var user = await _userRepository.GetByIdForAuthorizationAsync(userId);
        if (user == null) return null;

        return (user.RoleId, user.ClinicId);
    }

    public async Task<IEnumerable<(Guid UserId, Guid RoleId)>> GetUsersByRoleAsync(Guid roleId)
    {
        if (roleId == Guid.Empty) throw new ArgumentException("Role ID required", nameof(roleId));

        var users = await _userRepository.GetUsersByRoleIdAsync(roleId);
        return users.Select(u => (u.Id, u.RoleId)).ToList();
    }
}