using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Clinics;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly NabdCareDbContext _db;

    public SubscriptionRepository(NabdCareDbContext db)
    {
        _db = db;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions;

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Subscription>> GetByClinicIdAsync(Guid clinicId, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .Where(s => s.ClinicId == clinicId)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetAllAsync(bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetPagedAsync(int page, int pageSize, bool includePayments = false)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        IQueryable<Subscription> query = _db.Subscriptions
            .OrderByDescending(s => s.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await query.ToListAsync();
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        await _db.Subscriptions.AddAsync(subscription);
        await _db.SaveChangesAsync();
        return subscription;
    }

    public async Task<Subscription> UpdateAsync(Subscription subscription)
    {
        _db.Subscriptions.Update(subscription);
        await _db.SaveChangesAsync();
        return subscription;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
        if (subscription == null) return false;

        subscription.IsDeleted = true;
        _db.Subscriptions.Update(subscription);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
        if (subscription == null) return false;

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
        return true;
    }
}
