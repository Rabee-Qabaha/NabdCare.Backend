using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Application.Services.Permissions.Policies;

/// <summary>
/// ABAC policy for roles.
/// - SuperAdmins manage all roles.
/// - ClinicAdmins can manage roles limited to their own clinic.
/// </summary>
public class RolePolicy : IAccessPolicy<Role>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Role role)
    {
        if (user.IsSuperAdmin)
            return Task.FromResult(true);

        // Only allow management of roles in the same clinic (if applicable)
        if (user.ClinicId.HasValue && role.ClinicId == user.ClinicId)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }
}