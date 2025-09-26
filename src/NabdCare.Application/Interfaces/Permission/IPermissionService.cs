using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Users;

public interface IPermissionService
{
    // CRUD
    Task<IEnumerable<Domain.Entities.User.Permission>> GetAllPermissionsAsync();
    Task<Domain.Entities.User.Permission?> GetPermissionByIdAsync(Guid id);
    Task<Domain.Entities.User.Permission> CreatePermissionAsync(Domain.Entities.User.Permission permission);
    Task<Domain.Entities.User.Permission?> UpdatePermissionAsync(Guid id, Domain.Entities.User.Permission permission);
    Task<bool> DeletePermissionAsync(Guid id);

    // RolePermission
    Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId);

    // UserPermission
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    // Effective Permissions
    Task<IEnumerable<Domain.Entities.User.Permission>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role);
    Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName);
}