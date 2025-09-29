using NabdCare.Application.DTOs.Permissions;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionService
{
    // CRUD
    Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync();
    Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id);
    Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto);
    Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto);
    Task<bool> DeletePermissionAsync(Guid id);

    // RolePermission
    Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(UserRole role, Guid permissionId);

    // UserPermission
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    // Effective Permissions
    Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, UserRole role);
    Task<bool> UserHasPermissionAsync(Guid userId, UserRole role, string permissionName);
}