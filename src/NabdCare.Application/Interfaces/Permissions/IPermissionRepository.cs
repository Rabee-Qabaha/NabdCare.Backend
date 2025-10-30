using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionRepository
{
    // ============================================
    // PAGINATED QUERIES
    // ============================================

    /// <summary>
    /// Get all permissions (paginated, with optional filter and sort).
    /// </summary>
    Task<PaginatedResult<AppPermission>> GetAllPagedAsync(PaginationRequestDto pagination);

    // ============================================
    // BASIC CRUD
    // ============================================

    Task<IEnumerable<AppPermission>> GetAllPermissionsAsync();
    Task<AppPermission?> GetPermissionByIdAsync(Guid permissionId);
    Task<AppPermission> CreatePermissionAsync(AppPermission appPermission);
    Task<AppPermission?> UpdatePermissionAsync(Guid permissionId, AppPermission appPermission);
    Task<bool> DeletePermissionAsync(Guid permissionId);

    // ============================================
    // SEARCH
    // ============================================

    Task<IEnumerable<AppPermission>> SearchAsync(string query);

    // ============================================
    // ROLE PERMISSIONS
    // ============================================

    Task<IEnumerable<AppPermission>> GetPermissionsByRoleAsync(Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);

    // ============================================
    // USER PERMISSIONS
    // ============================================

    Task<IEnumerable<AppPermission>> GetPermissionsByUserAsync(Guid userId);
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);
}