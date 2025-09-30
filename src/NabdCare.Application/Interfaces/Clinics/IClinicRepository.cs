using NabdCare.Domain.Entities.Clinic;

namespace NabdCare.Application.Interfaces.Clinics;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id);
    Task<IEnumerable<Clinic>> GetAllAsync();
    Task<IEnumerable<Clinic>> GetPagedAsync(int page, int pageSize);

    // Existence checks used by service to enforce uniqueness (only active/non-deleted)
    Task<bool> ExistsByNameAsync(string name);
    Task<bool> ExistsByEmailAsync(string email);

    Task<Clinic> CreateAsync(Clinic clinic);
    Task<Clinic> UpdateAsync(Clinic clinic);

    // soft & hard delete
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}