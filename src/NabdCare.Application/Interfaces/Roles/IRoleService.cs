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
    /// SuperAdmin: All roles (system, templates, clinic-specific)
    /// ClinicAdmin: Template roles + their clinic's roles
    /// </summary>
    Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync();
    
    /// <summary>
    /// Get system roles (SuperAdmin, SupportManager, BillingManager).
    /// Only accessible to SuperAdmin.
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
    
    /// <summary>
    /// Create a new custom role.
    /// SuperAdmin: Can create system roles or clinic-specific roles for any clinic.
    /// ClinicAdmin: Can create roles only for their clinic.
    /// </summary>
    Task<RoleResponseDto> CreateRoleAsync(CreateRoleRequestDto dto);
    
    /// <summary>
    /// Clone a template role for a clinic.
    /// Optionally copies all permissions from the template.
    /// </summary>
    Task<RoleResponseDto> CloneRoleAsync(Guid templateRoleId, Guid? targetClinicId, string? newRoleName);
    
    /// <summary>
    /// Update role details.
    /// Cannot update system roles.
    /// ClinicAdmin can only update roles in their clinic.
    /// </summary>
    Task<RoleResponseDto?> UpdateRoleAsync(Guid id, UpdateRoleRequestDto dto);
    
    /// <summary>
    /// Delete a role.
    /// Cannot delete:
    /// - System roles
    /// - Roles with users assigned
    /// - Template roles (must convert to non-template first)
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid id);

    // ============================================
    // PERMISSION MANAGEMENT
    // ============================================
    
    /// <summary>
    /// Assign a permission to a role.
    /// </summary>
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    
    /// <summary>
    /// Remove a permission from a role.
    /// </summary>
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    
    /// <summary>
    /// Bulk assign multiple permissions to a role.
    /// </summary>
    Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    
    /// <summary>
    /// Sync role permissions (replace all existing with new set).
    /// </summary>
    Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
}