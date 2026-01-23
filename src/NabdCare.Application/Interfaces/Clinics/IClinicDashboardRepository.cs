using NabdCare.Application.DTOs.Clinics;

namespace NabdCare.Application.Interfaces.Clinics;

public interface IClinicDashboardRepository
{
    /// <summary>
    /// Calculates aggregated statistics for the clinic dashboard.
    /// Returns null if clinic not found.
    /// </summary>
    Task<ClinicDashboardStatsDto?> GetClinicStatsAsync(Guid clinicId);
}