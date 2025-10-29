using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Permissions.Policies;

public class ClinicPolicy : IAccessPolicy<Clinic>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Clinic clinic)
    {
        if (user.IsSuperAdmin) return Task.FromResult(true);
        var allowed = user.ClinicId.HasValue && user.ClinicId.Value == clinic.Id;
        return Task.FromResult(allowed);
    }
}