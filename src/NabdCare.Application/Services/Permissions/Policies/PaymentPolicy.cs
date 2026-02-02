using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Payments;

namespace NabdCare.Application.Services.Permissions.Policies;

public class PaymentPolicy : IAccessPolicy<Payment>
{
    // Note: The interface passes the context in EvaluateAsync, so we don't strictly need to inject it,
    // but keeping the constructor injection is fine if other dependencies are needed.
    // However, to match the interface signature exactly:

    public Task<bool> EvaluateAsync(ITenantContext context, string action, Payment resource)
    {
        // 1. SuperAdmin can access everything
        if (context.IsSuperAdmin)
        {
            return Task.FromResult(true);
        }

        // 2. If resource is null (e.g. list view check), allow access 
        // (The query filter will handle the actual data filtering)
        if (resource == null)
        {
            return Task.FromResult(true);
        }

        // 3. Clinic Admin / Staff can only access payments belonging to their clinic
        if (context.ClinicId.HasValue && resource.ClinicId == context.ClinicId.Value)
        {
            return Task.FromResult(true);
        }

        // 4. Deny otherwise
        return Task.FromResult(false);
    }
}