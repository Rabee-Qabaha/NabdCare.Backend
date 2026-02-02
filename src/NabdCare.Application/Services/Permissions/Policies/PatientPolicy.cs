using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Patients;

namespace NabdCare.Application.Services.Permissions.Policies;

public class PatientPolicy : IAccessPolicy<Patient>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Patient resource)
    {
        // 1. SuperAdmin can access everything
        if (user.IsSuperAdmin)
            return Task.FromResult(true);

        // 2. Allow list access (filtering happens in Repo)
        if (resource == null)
            return Task.FromResult(true);

        // 3. Users must belong to a clinic
        if (!user.ClinicId.HasValue)
            return Task.FromResult(false);

        // 4. Patient must belong to the same clinic
        if (resource.ClinicId == user.ClinicId.Value)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }
}