using NabdCare.Application.DTOs.Clinics;

namespace NabdCare.Application.Interfaces.Clinics;

public interface IClinicDashboardService
{
    Task<ClinicDashboardStatsDto> GetStatsAsync(Guid clinicId);
}