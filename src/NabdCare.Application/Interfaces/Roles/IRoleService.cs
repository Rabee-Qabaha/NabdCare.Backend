using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Roles;

namespace NabdCare.Application.Interfaces.Roles;

/// <summary>
/// Service for managing roles in the multi-tenant system.
/// Handles system roles, template roles, and clinic-specific roles.
/// </summary>
public interface IRoleService
{
    // ============================================
    // QUERY METHODS
    // ============================================

    /// <summary>
    /// Get all roles accessible to current user.
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync();

    /// <summary>
    /// Get all roles accessible to current user (paginated).
    /// </summary>
    Task<PaginatedResult<RoleResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination);

    /// <summary>
    /// Get system roles (SuperAdmin only).
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetSystemRolesAsync();

    /// <summary>
    /// Get template roles that can be cloned by clinics.
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetTemplateRolesAsync();

    /// <summary>
    /// Get roles for a specific clinic.
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetClinicRolesAsync(Guid clinicId);

    /// <summary>
    /// Get roles for a specific clinic (paginated).
    /// </summary>
    Task<PaginatedResult<RoleResponseDto>> GetClinicRolesPagedAsync(Guid clinicId, PaginationRequestDto pagination);

    /// <summary>
    /// Get role by ID.
    /// </summary>
    Task<RoleResponseDto?> GetRoleByIdAsync(Guid id);

    /// <summary>
    /// Get all permissions assigned to a role.
    /// </summary>
    Task<IEnumerable<string>> GetRolePermissionsAsync(Guid roleId);

    // ============================================
    // COMMAND METHODS
    // ============================================

    Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto dto);
    Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, Guid? targetClinicId, string? newRoleName);
    Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto);
    Task<bool> DeleteRoleAsync(Guid id);

    // ============================================
    // PERMISSION MANAGEMENT
    // ============================================

    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
}