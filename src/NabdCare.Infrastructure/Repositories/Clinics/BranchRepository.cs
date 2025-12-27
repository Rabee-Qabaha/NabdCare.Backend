using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Clinics.Branches;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Clinics;

public class BranchRepository : IBranchRepository
{
    private readonly NabdCareDbContext _db;

    public BranchRepository(NabdCareDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// unified method to get branches.
    /// If clinicId is provided, returns branches for that clinic.
    /// If clinicId is null, returns ALL branches (SuperAdmin view).
    /// </summary>
    public async Task<List<Branch>> GetListAsync(Guid? clinicId = null)
    {
        var query = _db.Branches.AsNoTracking().AsQueryable();

        // Filter by Clinic if ID is provided
        if (clinicId.HasValue)
        {
            query = query.Where(b => b.ClinicId == clinicId.Value);
        }

        // Apply global filters and sorting
        // Main branches always appear first in the list
        return await query
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.IsMain) 
            .ThenBy(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Branch?> GetByIdAsync(Guid id)
    {
        return await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<int> CountByClinicIdAsync(Guid clinicId)
    {
        return await _db.Branches
            .AsNoTracking()
            .CountAsync(b => b.ClinicId == clinicId && !b.IsDeleted);
    }

    public async Task<Branch> CreateAsync(Branch branch)
    {
        await _db.Branches.AddAsync(branch);
        await _db.SaveChangesAsync();
        return branch;
    }

    public async Task<Branch> UpdateAsync(Branch branch)
    {
        _db.Branches.Update(branch);
        await _db.SaveChangesAsync();
        return branch;
    }

    public async Task DeleteAsync(Branch branch)
    {
        // Soft Delete Logic
        branch.IsDeleted = true;
        branch.DeletedAt = DateTime.UtcNow;
        
        _db.Branches.Update(branch);
        await _db.SaveChangesAsync();
    }

    public async Task UnsetMainBranchAsync(Guid clinicId)
    {
        // Find the specific branch that is currently Main for this clinic
        var currentMain = await _db.Branches
            .FirstOrDefaultAsync(b => b.ClinicId == clinicId && b.IsMain && !b.IsDeleted);

        if (currentMain != null)
        {
            currentMain.IsMain = false;
            currentMain.UpdatedAt = DateTime.UtcNow;
            
            _db.Branches.Update(currentMain);
            await _db.SaveChangesAsync();
        }
    }
}