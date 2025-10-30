using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Permissions;

/// <summary>
/// Handles all application-level permission logic (CRUD + role/user linking).
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    // ============================================
    // PAGINATED QUERIES
    // ============================================

    public async Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
    {
        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve paged permissions.");
            throw new InvalidOperationException("Failed to retrieve paginated permissions.", ex);
        }
    }

    // ============================================
    // BASIC CRUD
    // ============================================

    public async Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
    {
        try
        {
            var permissions = await _permissionRepository.GetAllPermissionsAsync();
            return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all permissions.");
            throw new InvalidOperationException("Failed to retrieve permissions.", ex);
        }
    }

    public async Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
    {
        try
        {
            var permission = await _permissionRepository.GetPermissionByIdAsync(id);
            return permission == null ? null : _mapper.Map<PermissionResponseDto>(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve permission {PermissionId}.", id);
            throw new InvalidOperationException($"Failed to retrieve permission {id}.", ex);
        }
    }

    public async Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Permission name is required.", nameof(dto.Name));

        try
        {
            var entity = _mapper.Map<AppPermission>(dto);
            var created = await _permissionRepository.CreatePermissionAsync(entity);
            return _mapper.Map<PermissionResponseDto>(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create permission {PermissionName}.", dto.Name);
            throw new InvalidOperationException($"Failed to create permission {dto.Name}.", ex);
        }
    }

    public async Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
    {
        try
        {
            var entity = _mapper.Map<AppPermission>(dto);
            var updated = await _permissionRepository.UpdatePermissionAsync(id, entity);
            return updated == null ? null : _mapper.Map<PermissionResponseDto>(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update permission {PermissionId}.", id);
            throw new InvalidOperationException($"Failed to update permission {id}.", ex);
        }
    }

    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        try
        {
            return await _permissionRepository.DeletePermissionAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete permission {PermissionId}.", id);
            throw new InvalidOperationException($"Failed to delete permission {id}.", ex);
        }
    }

    // ============================================
    // ROLE PERMISSIONS
    // ============================================

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId)
    {
        try
        {
            var permissions = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
            return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve permissions for role {RoleId}.", roleId);
            throw new InvalidOperationException("Failed to retrieve role permissions.", ex);
        }
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role {roleId} not found.");

            return await _permissionRepository.AssignPermissionToRoleAsync(roleId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permission {PermissionId} to role {RoleId}.", permissionId, roleId);
            throw new InvalidOperationException("Failed to assign permission to role.", ex);
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleRepository.GetRoleByIdAsync(roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role {roleId} not found.");

            return await _permissionRepository.RemovePermissionFromRoleAsync(roleId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove permission {PermissionId} from role {RoleId}.", permissionId, roleId);
            throw new InvalidOperationException("Failed to remove permission from role.", ex);
        }
    }

    // ============================================
    // USER PERMISSIONS
    // ============================================

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId)
    {
        try
        {
            var permissions = await _permissionRepository.GetPermissionsByUserAsync(userId);
            return _mapper.Map<IEnumerable<PermissionResponseDto>>(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve permissions for user {UserId}.", userId);
            throw new InvalidOperationException("Failed to retrieve user permissions.", ex);
        }
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        try
        {
            return await _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            if (message.Contains("Users") && message.Contains("foreign key"))
                throw new KeyNotFoundException($"User with ID {userId} does not exist.");
            if (message.Contains("Permissions") && message.Contains("foreign key"))
                throw new KeyNotFoundException($"Permission with ID {permissionId} does not exist.");

            _logger.LogError(ex, "Database error assigning permission {PermissionId} to user {UserId}.", permissionId, userId);
            throw new InvalidOperationException("Failed to assign permission to user.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permission {PermissionId} to user {UserId}.", permissionId, userId);
            throw new InvalidOperationException("Failed to assign permission to user.", ex);
        }
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        try
        {
            return await _permissionRepository.RemovePermissionFromUserAsync(userId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove permission {PermissionId} from user {UserId}.", permissionId, userId);
            throw new InvalidOperationException("Failed to remove permission from user.", ex);
        }
    }

    // ============================================
    // EFFECTIVE PERMISSIONS
    // ============================================

    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        try
        {
            var rolePerms = await _permissionRepository.GetPermissionsByRoleAsync(roleId);
            var userPerms = await _permissionRepository.GetPermissionsByUserAsync(userId);

            var combined = rolePerms.Concat(userPerms)
                .GroupBy(p => p.Id)
                .Select(g => g.First());

            return _mapper.Map<IEnumerable<PermissionResponseDto>>(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch effective permissions for user {UserId}.", userId);
            throw new InvalidOperationException("Failed to fetch effective permissions.", ex);
        }
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name is required.", nameof(permissionName));

        var effective = await GetUserEffectivePermissionsAsync(userId, roleId);
        return effective.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdRawAsync(userId); // Ignoring filters
        if (user == null) return null;

        return (user.RoleId, user.ClinicId);
    }
}