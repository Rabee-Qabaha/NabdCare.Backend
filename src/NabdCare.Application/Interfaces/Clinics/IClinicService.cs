using NabdCare.Application.DTOs.Clinics;

namespace NabdCare.Application.Interfaces.Clinics;

public interface IClinicService
{
    Task<ClinicResponseDto> CreateClinicAsync(CreateClinicRequestDto dto);
    Task<ClinicResponseDto?> GetClinicByIdAsync(Guid id);
    Task<IEnumerable<ClinicResponseDto>> GetAllClinicsAsync();
    Task<IEnumerable<ClinicResponseDto>> GetPagedClinicsAsync(int page, int pageSize);
    Task<ClinicResponseDto?> UpdateClinicAsync(Guid id, UpdateClinicRequestDto dto);
    Task<bool> SoftDeleteClinicAsync(Guid id);
    Task<bool> DeleteClinicAsync(Guid id);
}