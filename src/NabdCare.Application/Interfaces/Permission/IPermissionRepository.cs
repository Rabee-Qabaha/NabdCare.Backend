using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Permission;

public interface IPermissionRepository
{
    Task<IEnumerable<Domain.Entities.User.Permission>> GetAllPermissionsAsync();
    Task<Domain.Entities.User.Permission?> GetPermissionByIdAsync(Guid permissionId);
    Task<Domain.Entities.User.Permission> CreatePermissionAsync(Domain.Entities.User.Permission permission);
    Task<Domain.Entities.User.Permission?> UpdatePermissionAsync(Guid permissionId, Domain.Entities.User.Permission permission);
    Task<bool> DeletePermissionAsync(Guid permissionId);

    Task<IEnumerable<Domain.Entities.User.Permission>> GetPermissionsByRoleAsync(UserRole role);
    Task<IEnumerable<Domain.Entities.User.Permission>> GetPermissionsByUserAsync(Guid userId);

    Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId);
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
}