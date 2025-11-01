using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Permissions.Policies;

/// <summary>
/// ABAC policy for subscriptions.
/// - SuperAdmins can view/manage all subscriptions.
/// - ClinicAdmins can only view/manage subscriptions for their clinic.
/// </summary>
public class SubscriptionPolicy : IAccessPolicy<Subscription>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Subscription subscription)
    {
        if (user.IsSuperAdmin)
            return Task.FromResult(true);

        // Restrict by clinic ownership
        if (user.ClinicId.HasValue && subscription.ClinicId == user.ClinicId)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }
}