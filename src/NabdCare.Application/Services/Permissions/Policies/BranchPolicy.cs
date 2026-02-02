using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Permissions.Policies;

public class BranchPolicy : IAccessPolicy<Branch>
{
    public Task<bool> EvaluateAsync(ITenantContext user, string action, Branch branch)
    {
        // 1. SuperAdmin can do anything
        if (user.IsSuperAdmin) return Task.FromResult(true);

        // 2. Allow list access (filtering happens in Repo)
        if (branch == null) return Task.FromResult(true);

        // 3. Clinic Admin can only access branches BELONGING to their clinic
        if (user.ClinicId.HasValue && branch.ClinicId == user.ClinicId.Value)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}