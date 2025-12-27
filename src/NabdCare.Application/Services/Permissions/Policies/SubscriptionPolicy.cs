using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Subscriptions;

namespace NabdCare.Application.Services.Permissions.Policies;

public class SubscriptionPolicy : IAccessPolicy<Subscription>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Subscription resource)
    {
        // 1. SuperAdmin can do anything
        if (user.IsSuperAdmin)
            return Task.FromResult(true);

        // 2. Users must belong to a clinic
        if (!user.ClinicId.HasValue)
            return Task.FromResult(false);

        // 3. Subscription must belong to user's clinic
        if (resource.ClinicId != user.ClinicId.Value)
            return Task.FromResult(false);

        // 4. Action-specific rules for Clinic Admins
        switch (action)
        {
            case "view":
            case "viewActive":
            case "list":
                return Task.FromResult(true); // Can see own

            case "edit": // Clinic Admins usually can't edit plan details, only SuperAdmin
                return Task.FromResult(false); 

            case "create": // Only SuperAdmin creates subs manually
                return Task.FromResult(false);

            case "cancel": // Clinic Admin might be allowed to cancel
                return Task.FromResult(true);

            default:
                return Task.FromResult(false);
        }
    }
}