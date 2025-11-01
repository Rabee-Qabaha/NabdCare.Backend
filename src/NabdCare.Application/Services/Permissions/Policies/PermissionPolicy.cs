using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Permissions.Policies;

public class PermissionPolicy : IAccessPolicy<AppPermission>
{
    public Task<bool> EvaluateAsync(ITenantContext tenant, string action, AppPermission permission)
    {
        // Only SuperAdmin or system roles can edit system permissions
        return Task.FromResult(tenant.IsSuperAdmin);
    }
}