using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionRepository
{
    // CRUD
    Task<IEnumerable<AppPermission>> GetAllPermissionsAsync();
    Task<AppPermission?> GetPermissionByIdAsync(Guid permissionId);
    Task<AppPermission> CreatePermissionAsync(AppPermission appPermission);
    Task<AppPermission?> UpdatePermissionAsync(Guid permissionId, AppPermission appPermission);
    Task<bool> DeletePermissionAsync(Guid permissionId);

    // Role Permissions
    Task<IEnumerable<AppPermission>> GetPermissionsByRoleAsync(UserRole role);
    Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId);

    // User Permissions
    Task<IEnumerable<AppPermission>> GetPermissionsByUserAsync(Guid userId);
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);
}