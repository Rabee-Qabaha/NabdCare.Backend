
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionRepository
{
    // CRUD
    Task<IEnumerable<AppPermission>> GetAllPermissionsAsync();
    Task<AppPermission?> GetPermissionByIdAsync(Guid permissionId);
    Task<AppPermission> CreatePermissionAsync(AppPermission appPermission);
    Task<AppPermission?> UpdatePermissionAsync(Guid permissionId, AppPermission appPermission);
    Task<bool> DeletePermissionAsync(Guid permissionId);

    Task<IEnumerable<AppPermission>> GetPermissionsByRoleAsync(Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);

    // User Permissions
    Task<IEnumerable<AppPermission>> GetPermissionsByUserAsync(Guid userId);
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);
}