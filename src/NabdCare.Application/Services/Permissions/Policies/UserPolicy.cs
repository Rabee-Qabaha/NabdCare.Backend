using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Services.Permissions.Policies;

/// <summary>
/// ABAC policy for user management.
/// - SuperAdmins can manage any user.
/// - ClinicAdmins can manage users in their own clinic.
/// - Any user can always access their own profile.
/// </summary>
public class UserPolicy : IAccessPolicy<User>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, User targetUser)
    {
        if (user.IsSuperAdmin)
            return Task.FromResult(true);

        // ClinicAdmin can manage users within the same clinic
        if (user.ClinicId.HasValue && targetUser.ClinicId == user.ClinicId)
            return Task.FromResult(true);

        // A user can always view or edit their own account
        if (user.UserId == targetUser.Id)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }
}