using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Application.Validator.Clinics.Branches;

namespace NabdCare.Application.Interfaces.Clinics.Branches;

public interface IBranchService
{
    Task<List<BranchResponseDto>> GetBranchesAsync(Guid? clinicId = null);

    Task<BranchResponseDto?> GetBranchByIdAsync(Guid id);
    Task<BranchResponseDto> CreateBranchAsync(CreateBranchRequestDto dto);
    Task<BranchResponseDto> UpdateBranchAsync(Guid id, UpdateBranchRequestDto dto);
    Task DeleteBranchAsync(Guid id);
}