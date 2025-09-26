using NabdCare.Domain.Enums;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionService
{
    // CRUD
    Task<IEnumerable<AppPermission>> GetAllPermissionsAsync();
    Task<AppPermission?> GetPermissionByIdAsync(Guid id);
    Task<AppPermission> CreatePermissionAsync(AppPermission permission);
    Task<AppPermission?> UpdatePermissionAsync(Guid id, AppPermission permission);
    Task<bool> DeletePermissionAsync(Guid id);

    // RolePermission
    Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId);

    // UserPermission
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    // Effective Permissions
    Task<IEnumerable<AppPermission>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role);
    Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName);
}