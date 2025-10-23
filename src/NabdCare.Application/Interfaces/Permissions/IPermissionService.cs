using NabdCare.Application.DTOs.Permissions;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionService
{
    // CRUD
    Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync();
    Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId);
    Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId);
    Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id);
    Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto);
    Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto);
    Task<bool> DeletePermissionAsync(Guid id);

    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);

    // UserPermission
    Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId);
    Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId);

    Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId);
    Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName);
}