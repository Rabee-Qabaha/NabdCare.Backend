using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinic;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Clinics;

public class ClinicRepository : IClinicRepository
{
    private readonly NabdCareDbContext _db;

    public ClinicRepository(NabdCareDbContext db)
    {
        _db = db;
    }

    public async Task<Clinic?> GetByIdAsync(Guid id)
        => await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Clinic>> GetAllAsync()
        => await _db.Clinics.OrderBy(c => c.Name).ToListAsync();

    public async Task<IEnumerable<Clinic>> GetPagedAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        return await _db.Clinics
                        .OrderBy(c => c.Name)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim().ToLower();
        return await _db.Clinics.AnyAsync(c => c.Name.ToLower() == n);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var e = email.Trim().ToLower();
        return await _db.Clinics.AnyAsync(c => c.Email != null && c.Email.ToLower() == e);
    }

    public async Task<Clinic> CreateAsync(Clinic clinic)
    {
        await _db.Clinics.AddAsync(clinic);
        await _db.SaveChangesAsync();
        return clinic;
    }

    public async Task<Clinic> UpdateAsync(Clinic clinic)
    {
        _db.Clinics.Update(clinic);
        await _db.SaveChangesAsync();
        return clinic;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (clinic == null) return false;

        clinic.IsDeleted = true;
        _db.Clinics.Update(clinic);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return false;

        _db.Clinics.Remove(clinic);
        await _db.SaveChangesAsync();
        return true;
    }
}