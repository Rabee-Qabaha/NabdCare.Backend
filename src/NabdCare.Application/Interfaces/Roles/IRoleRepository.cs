using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Application.Interfaces.Roles;

public interface IRoleRepository
{
    // ============================================
    // QUERY METHODS
    // ============================================
    Task<PaginatedResult<Role>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includeDeleted = false,
        Func<IQueryable<Role>, IQueryable<Role>>? abacFilter = null);

    Task<PaginatedResult<Role>> GetClinicRolesPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includeDeleted = false,
        Func<IQueryable<Role>, IQueryable<Role>>? abacFilter = null);

    Task<IEnumerable<Role>> GetAllRolesAsync(bool includeDeleted = false, Guid? clinicId = null);
    Task<IEnumerable<Role>> GetSystemRolesAsync();
    Task<IEnumerable<Role>> GetTemplateRolesAsync();

    Task<Role?> GetRoleByIdAsync(Guid id);
    Task<Role?> GetRoleByNameAsync(string name, Guid? clinicId = null);

    Task<int> GetRoleUserCountAsync(Guid roleId);
    Task<int> GetRolePermissionCountAsync(Guid roleId);
    Task<bool> RoleExistsAsync(Guid id);
    Task<bool> RoleNameExistsAsync(string name, Guid? clinicId, Guid? excludeRoleId = null);

    // ============================================
    // COMMAND METHODS
    // ============================================
    Task<Role> CreateRoleAsync(Role role);
    Task<Role?> UpdateRoleAsync(Role role);
    Task<Role?> DeleteRoleAsync(Guid id);
    Task<Role?> RestoreRoleAsync(Guid id);

    // ============================================
    // PERMISSION MANAGEMENT
    // ============================================
    Task<IEnumerable<Guid>> GetRolePermissionIdsAsync(Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
}