using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Application.Interfaces.Roles;

public interface IRoleRepository
{
    Task<PaginatedResult<Role>> GetAllPagedAsync(
        RoleFilterRequestDto filter, 
        Func<IQueryable<Role>, IQueryable<Role>>? abacFilter = null);

    Task<IEnumerable<Role>> GetAllRolesAsync(
        RoleFilterRequestDto filter, 
        Func<IQueryable<Role>, IQueryable<Role>>? abacFilter = null);

    Task<IEnumerable<Role>> GetSystemRolesAsync();
    Task<IEnumerable<Role>> GetTemplateRolesAsync();
    Task<Role?> GetRoleByIdAsync(Guid id);
    Task<Role?> GetRoleByNameAsync(string name, Guid? clinicId = null);

    Task<int> GetRoleUserCountAsync(Guid roleId);
    Task<int> GetRolePermissionCountAsync(Guid roleId);
    Task<bool> RoleExistsAsync(Guid id);
    Task<bool> RoleNameExistsAsync(string name, Guid? clinicId, Guid? excludeRoleId = null);

    // Commands
    Task<Role> CreateRoleAsync(Role role);
    Task<Role?> UpdateRoleAsync(Role role);
    Task<bool> SoftDeleteRoleAsync(Guid id);
    Task<bool> HardDeleteRoleAsync(Guid id);
    Task<Role?> RestoreRoleAsync(Guid id);

    // Permissions
    Task<IEnumerable<Guid>> GetRolePermissionIdsAsync(Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
}