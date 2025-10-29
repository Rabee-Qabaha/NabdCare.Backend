namespace NabdCare.Application.Interfaces.Permissions;

public interface IPermissionCacheInvalidator
{
    Task InvalidateUserAsync(Guid userId);
    Task InvalidateRoleAsync(Guid roleId);
}