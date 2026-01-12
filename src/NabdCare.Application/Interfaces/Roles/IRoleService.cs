using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Roles;

namespace NabdCare.Application.Interfaces.Roles;

/// <summary>
/// Service for managing roles in the multi-tenant system.
/// Handles system roles, template roles, and clinic-specific roles.
/// 
/// Author: Rabee-Qabaha
/// Updated: 2025-11-10 19:41:57 UTC
/// </summary>
public interface IRoleService
{
    // ============================================
    // QUERY METHODS
    // ============================================

    /// <summary>
    /// Get all roles accessible to current user with unified filtering.
    /// Returns a flat list (ideal for Dropdowns).
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync(RoleFilterRequestDto filter);

    /// <summary>
    /// Get all roles accessible to current user with unified filtering.
    /// Returns a paginated result (ideal for Grids/Tables).
    /// </summary>
    Task<PaginatedResult<RoleResponseDto>> GetAllPagedAsync(RoleFilterRequestDto filter);

    /// <summary>
    /// Get system roles (SuperAdmin only).
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetSystemRolesAsync();

    /// <summary>
    /// Get template roles that can be cloned by clinics.
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetTemplateRolesAsync();

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
    
    Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, CloneRoleRequestDto dto);
    
    Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto);
    
    /// <summary>
    /// Soft delete a role. Cannot delete system roles or roles with assigned users.
    /// </summary>
    Task<bool> SoftDeleteRoleAsync(Guid id);
    
    /// <summary>
    /// Hard delete a role. Cannot delete system roles or roles with assigned users.
    /// </summary>
    Task<bool> HardDeleteRoleAsync(Guid id);
    
    /// <summary>
    /// Restore a soft-deleted role.
    /// </summary>
    Task<RoleResponseDto?> RestoreRoleAsync(Guid id);

    // ============================================
    // PERMISSION MANAGEMENT
    // ============================================

    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
}