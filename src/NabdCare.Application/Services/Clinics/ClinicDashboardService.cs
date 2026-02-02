using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Services.Clinics;

public class ClinicDashboardService : IClinicDashboardService
{
    private readonly IClinicDashboardRepository _dashboardRepo;
    private readonly ITenantContext _tenantContext;
    private readonly IAccessPolicy<Clinic> _policy;

    public ClinicDashboardService(
        IClinicDashboardRepository dashboardRepo,
        ITenantContext tenantContext,
        IAccessPolicy<Clinic> policy)
    {
        _dashboardRepo = dashboardRepo;
        _tenantContext = tenantContext;
        _policy = policy;
    }

    public async Task<ClinicDashboardStatsDto> GetStatsAsync(Guid clinicId)
    {
        var dummyClinic = new Clinic { Id = clinicId };
        if (!await _policy.EvaluateAsync(_tenantContext, "read", dummyClinic))
             throw new UnauthorizedAccessException("Access denied to this clinic's dashboard.");

        var stats = await _dashboardRepo.GetClinicStatsAsync(clinicId);

        if (stats == null)
        {
            throw new KeyNotFoundException($"Clinic with ID {clinicId} not found.");
        }

        return stats;
    }
}