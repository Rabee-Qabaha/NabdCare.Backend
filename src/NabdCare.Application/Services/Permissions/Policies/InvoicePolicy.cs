using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Application.Services.Permissions.Policies;

/// <summary>
/// ABAC policy for Invoice management.
/// - SuperAdmins can access any invoice.
/// - Clinic users can only access invoices belonging to their clinic.
/// </summary>
public class InvoicePolicy : IAccessPolicy<Invoice>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Invoice resource)
    {
        // 1. SuperAdmin can do anything
        if (user.IsSuperAdmin)
            return Task.FromResult(true);

        // 2. Users must be authenticated and belong to a clinic
        if (!user.ClinicId.HasValue)
            return Task.FromResult(false);

        // 3. The invoice must belong to the user's clinic
        if (resource.ClinicId == user.ClinicId.Value)
            return Task.FromResult(true);

        // Default deny
        return Task.FromResult(false);
    }
}