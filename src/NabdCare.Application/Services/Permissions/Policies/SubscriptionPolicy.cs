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
        // Standardizing action names: "read", "write", "delete"
        switch (action.ToLower())
        {
            case "read":
            case "view":
            case "list":
                return Task.FromResult(true); // Can see own

            case "write": 
            case "edit":
            case "update":
            case "renew":
                // Clinic Admins can usually RENEW or UPGRADE (Write), but maybe not change core details.
                // For now, we allow it if they own it, relying on Service validation for specific fields.
                return Task.FromResult(true); 

            case "create": 
                // Only SuperAdmin creates subs manually via API, but Clinic Admin might trigger "Purchase"
                // which creates a sub. If the service calls this policy for a NEW sub, resource.ClinicId matches.
                return Task.FromResult(true);

            case "delete":
            case "cancel": 
                return Task.FromResult(true); // Can cancel own

            default:
                return Task.FromResult(false);
        }
    }
}