using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Interfaces.Clinics.Branches;

public interface IBranchRepository
{
    // ✅ Unified Method (Pass null for "All", pass Guid for "Specific Clinic")
    Task<List<Branch>> GetListAsync(Guid? clinicId = null);
    
    Task<Branch?> GetByIdAsync(Guid id);
    Task<int> CountByClinicIdAsync(Guid clinicId);
    Task<Branch> CreateAsync(Branch branch);
    Task<Branch> UpdateAsync(Branch branch);
    Task DeleteAsync(Branch branch);
    Task UnsetMainBranchAsync(Guid clinicId);
}