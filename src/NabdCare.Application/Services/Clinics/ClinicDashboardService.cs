using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;

namespace NabdCare.Application.Services.Clinics;

public class ClinicDashboardService : IClinicDashboardService
{
    // We replace the 4 separate repositories with the single specialized Dashboard Repository
    private readonly IClinicDashboardRepository _dashboardRepo;

    public ClinicDashboardService(IClinicDashboardRepository dashboardRepo)
    {
        _dashboardRepo = dashboardRepo;
    }

    public async Task<ClinicDashboardStatsDto> GetStatsAsync(Guid clinicId)
    {
        // The repository now handles fetching, parallel execution, 
        // growth rate calculations, and mapping to the DTO.
        var stats = await _dashboardRepo.GetClinicStatsAsync(clinicId);

        if (stats == null)
        {
            throw new KeyNotFoundException($"Clinic with ID {clinicId} not found.");
        }

        return stats;
    }
}