using NabdCare.Application.Interfaces.Permission;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Services.Users;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    #region Permission CRUD

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        => await _permissionRepository.GetAllPermissionsAsync();

    public async Task<Permission?> GetPermissionByIdAsync(Guid id)
        => await _permissionRepository.GetPermissionByIdAsync(id);

    public async Task<Permission> CreatePermissionAsync(Permission permission)
        => await _permissionRepository.CreatePermissionAsync(permission);

    public async Task<Permission?> UpdatePermissionAsync(Guid id, Permission permission)
        => await _permissionRepository.UpdatePermissionAsync(id, permission);

    public async Task<bool> DeletePermissionAsync(Guid id)
        => await _permissionRepository.DeletePermissionAsync(id);

    #endregion

    #region RolePermission Management

    public async Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId)
        => await _permissionRepository.AssignPermissionToRoleAsync(role, permissionId);

    public async Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId)
    {
        var rolePermission = (await _permissionRepository.GetPermissionsByRoleAsync(role))
            .FirstOrDefault(p => p.Id == permissionId);
        if (rolePermission == null) return false;

        // Remove RolePermission
        // Note: direct repository access for removal
        return await Task.Run(async () =>
        {
            using var db = _permissionRepository as dynamic;
            db._dbContext.RolePermissions.Remove(
                db._dbContext.RolePermissions.First(rp => rp.Role == role && rp.PermissionId == permissionId)
            );
            await db._dbContext.SaveChangesAsync();
            return true;
        });
    }

    #endregion

    #region UserPermission Management

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
        => await _permissionRepository.AssignPermissionToUserAsync(userId, permissionId);

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var userPermission = (await _permissionRepository.GetPermissionsByUserAsync(userId))
            .FirstOrDefault(p => p.Id == permissionId);
        if (userPermission == null) return false;

        return await Task.Run(async () =>
        {
            using var db = _permissionRepository as dynamic;
            db._dbContext.UserPermissions.Remove(
                db._dbContext.UserPermissions.First(up => up.UserId == userId && up.PermissionId == permissionId)
            );
            await db._dbContext.SaveChangesAsync();
            return true;
        });
    }

    #endregion

    #region Effective Permissions

    /// <summary>
    /// Returns all permissions a user has (role + user overrides)
    /// </summary>
    public async Task<IEnumerable<Permission>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role)
    {
        var rolePermissions = await _permissionRepository.GetPermissionsByRoleAsync(role);
        var userPermissions = await _permissionRepository.GetPermissionsByUserAsync(userId);

        // Combine and remove duplicates
        return rolePermissions.Concat(userPermissions)
            .GroupBy(p => p.Id)
            .Select(g => g.First());
    }

    /// <summary>
    /// Checks if a user has a specific permission (role + user)
    /// </summary>
    public async Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName)
    {
        var effectivePermissions = await GetUserEffectivePermissionsAsync(userId, role);
        return effectivePermissions.Any(p =>
            p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
