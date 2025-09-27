using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IUserRepository userRepository,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    #region Permission CRUD

    public async Task<IEnumerable<AppPermission>> GetAllPermissionsAsync()
    {
        try
        {
            return await _permissionRepository.GetAllPermissionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all permissions");
            throw new InvalidOperationException("An error occurred while fetching permissions.", ex);
        }
    }

    public async Task<AppPermission?> GetPermissionByIdAsync(Guid id)
    {
        try
        {
            return await _permissionRepository.GetPermissionByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permission {PermissionId}", id);
            throw new InvalidOperationException("An error occurred while fetching the permission.", ex);
        }
    }

    public async Task<AppPermission> CreatePermissionAsync(AppPermission permission)
    {
        try
        {
            return await _permissionRepository.CreatePermissionAsync(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {PermissionName}", permission.Name);
            throw new InvalidOperationException("An error occurred while creating the permission.", ex);
        }
    }

    public async Task<AppPermission?> UpdatePermissionAsync(Guid id, AppPermission permission)
    {
        try
        {
            return await _permissionRepository.UpdatePermissionAsync(id, permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", id);
            throw new InvalidOperationException("An error occurred while updating the permission.", ex);
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
            _logger.LogError(ex, "Error deleting permission {PermissionId}", id);
            throw new InvalidOperationException("An error occurred while deleting the permission.", ex);
        }
    }

    #endregion

    #region RolePermission Management

    public async Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId)
    {
        try
        {
            return await _permissionRepository.AssignPermissionToRoleAsync(role, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {Role}", permissionId, role);
            throw new InvalidOperationException("An error occurred while assigning permission to role.", ex);
        }
    }

    public async Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId)
    {
        try
        {
            return await _permissionRepository.RemovePermissionFromRoleAsync(role, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {Role}", permissionId, role);
            throw new InvalidOperationException("An error occurred while removing permission from role.", ex);
        }
    }

    #endregion

    #region UserPermission Management

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        try
        {
            // Optional: Validate user exists
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException($"User {userId} not found.");

            return await _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to user {UserId}", permissionId, userId);
            throw new InvalidOperationException("An error occurred while assigning permission to user.", ex);
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
            _logger.LogError(ex, "Error removing permission {PermissionId} from user {UserId}", permissionId, userId);
            throw new InvalidOperationException("An error occurred while removing permission from user.", ex);
        }
    }

    #endregion

    #region Effective Permissions

    public async Task<IEnumerable<AppPermission>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role)
    {
        try
        {
            var rolePermissions = await _permissionRepository.GetPermissionsByRoleAsync(role);
            var userPermissions = await _permissionRepository.GetPermissionsByUserAsync(userId);

            return rolePermissions.Concat(userPermissions)
                                  .GroupBy(p => p.Id)
                                  .Select(g => g.First());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching effective permissions for user {UserId}", userId);
            throw new InvalidOperationException("An error occurred while fetching effective permissions.", ex);
        }
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName)
    {
        try
        {
            var effectivePermissions = await GetUserEffectivePermissionsAsync(userId, role);
            return effectivePermissions.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {PermissionName} for user {UserId}", permissionName, userId);
            throw new InvalidOperationException("An error occurred while checking user permissions.", ex);
        }
    }

    #endregion
}