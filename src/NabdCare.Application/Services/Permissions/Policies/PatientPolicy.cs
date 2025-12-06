using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Patients;

namespace NabdCare.Application.Services.Permissions.Policies;

public class PatientPolicy : IAccessPolicy<Patient>
{
    // public Task<bool> EvaluateAsync(ITenantContext user, string action, Patient patient)
    // {
    //     if (user.ClinicId is null) return Task.FromResult(false);
    //     // Example rule: same clinic only
    //     var allowed = patient.ClinicId == user.ClinicId;
    //     return Task.FromResult(allowed);
    // }
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Patient resource)
    {
        throw new NotImplementedException();
    }
}