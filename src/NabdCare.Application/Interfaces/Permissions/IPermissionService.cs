using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionService
{
    // ============================================
    // PAGINATED QUERIES
    // ============================================

    Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination);

    // ============================================
    // BASIC CRUD
    // ============================================

    Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync();
    Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id);
    Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto);
    Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto);
    Task<bool> DeletePermissionAsync(Guid id);

    // ============================================
    // ROLE PERMISSIONS
    // ============================================

    Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId);
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);

    // ============================================
    // USER PERMISSIONS
    // ============================================

    Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId);
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    // ============================================
    // EFFECTIVE PERMISSIONS
    // ============================================

    Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId);
    Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName);
    Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId);
}