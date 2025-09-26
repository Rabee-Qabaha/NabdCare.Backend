using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRepository _userRepository;

    public PermissionService(IPermissionRepository permissionRepository, IUserRepository userRepository)
    {
        _permissionRepository = permissionRepository;
        _userRepository = userRepository;
    }

    #region Permission CRUD

    public Task<IEnumerable<AppPermission>> GetAllPermissionsAsync()
        => _permissionRepository.GetAllPermissionsAsync();

    public Task<AppPermission?> GetPermissionByIdAsync(Guid id)
        => _permissionRepository.GetPermissionByIdAsync(id);

    public Task<AppPermission> CreatePermissionAsync(AppPermission permission)
        => _permissionRepository.CreatePermissionAsync(permission);

    public Task<AppPermission?> UpdatePermissionAsync(Guid id, AppPermission permission)
        => _permissionRepository.UpdatePermissionAsync(id, permission);

    public Task<bool> DeletePermissionAsync(Guid id)
        => _permissionRepository.DeletePermissionAsync(id);

    #endregion

    #region RolePermission Management

    public Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId)
        => _permissionRepository.AssignPermissionToRoleAsync(role, permissionId);

    public Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId)
        => _permissionRepository.RemovePermissionFromRoleAsync(role, permissionId);

    #endregion

    #region UserPermission Management

    public Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
        => _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);

    public Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
        => _permissionRepository.RemovePermissionFromUserAsync(userId, permissionId);

    #endregion

    #region Effective Permissions

    /// <summary>
    /// Returns all effective permissions for a user (role + user-specific).
    /// </summary>
    public async Task<IEnumerable<AppPermission>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role)
    {
        var rolePermissions = await _permissionRepository.GetPermissionsByRoleAsync(role);
        var userPermissions = await _permissionRepository.GetPermissionsByUserAsync(userId);

        return rolePermissions.Concat(userPermissions)
            .GroupBy(p => p.Id)
            .Select(g => g.First());
    }

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    public async Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName)
    {
        var effectivePermissions = await GetUserEffectivePermissionsAsync(userId, role);

        return effectivePermissions.Any(p =>
            p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}