using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Clinics;

/// <summary>
/// Production-ready clinic repository.
/// Thin data access layer - no business logic, no try-catch.
/// </summary>
public class ClinicRepository : IClinicRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<ClinicRepository> _logger;

    public ClinicRepository(
        NabdCareDbContext dbContext,
        ILogger<ClinicRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

    public async Task<Clinic?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return null;

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<IEnumerable<Clinic>> GetAllAsync()
    {
        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> GetByStatusAsync(SubscriptionStatus status)
    {
        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted && c.Status == status)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> GetActiveWithValidSubscriptionAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted && 
                c.Status == SubscriptionStatus.Active &&
                c.Subscriptions.Any(s => 
                    !s.IsDeleted && 
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > now))
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> GetWithExpiringSubscriptionsAsync(int withinDays)
    {
        var now = DateTime.UtcNow;
        var expirationDate = now.AddDays(withinDays);

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted && 
                c.Status == SubscriptionStatus.Active &&
                c.Subscriptions.Any(s => 
                    !s.IsDeleted && 
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > now && 
                    s.EndDate <= expirationDate))
            .OrderBy(c => c.Subscriptions
                .Where(s => !s.IsDeleted && s.Status == SubscriptionStatus.Active)
                .Min(s => s.EndDate))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> GetWithExpiredSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted && 
                c.Subscriptions.Any(s => 
                    !s.IsDeleted && 
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate <= now))
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> GetPagedAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Clinic>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<Clinic>();

        var searchTerm = query.Trim().ToLower();

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted && 
                (c.Name.ToLower().Contains(searchTerm) ||
                 (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                 (c.Phone != null && c.Phone.Contains(searchTerm))))
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalizedName = name.Trim().ToLower();

        var query = _dbContext.Clinics
            .Where(c => !c.IsDeleted && c.Name.ToLower() == normalizedName);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AsNoTracking().AnyAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLower();

        var query = _dbContext.Clinics
            .Where(c => !c.IsDeleted && c.Email != null && c.Email.ToLower() == normalizedEmail);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AsNoTracking().AnyAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        return await _dbContext.Clinics
            .AsNoTracking()
            .AnyAsync(c => c.Id == id && !c.IsDeleted);
    }

    #endregion

    #region COMMAND METHODS

    public async Task<Clinic> CreateAsync(Clinic clinic)
    {
        if (clinic == null)
            throw new ArgumentNullException(nameof(clinic));

        await _dbContext.Clinics.AddAsync(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Clinic {ClinicId} created in database", clinic.Id);

        return clinic;
    }

    public async Task<Clinic> UpdateAsync(Clinic clinic)
    {
        if (clinic == null)
            throw new ArgumentNullException(nameof(clinic));

        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Clinic {ClinicId} updated in database", clinic.Id);

        return clinic;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        var clinic = await _dbContext.Clinics
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (clinic == null)
            return false;

        clinic.IsDeleted = true;
        clinic.DeletedAt = DateTime.UtcNow;

        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Clinic {ClinicId} soft deleted in database", id);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null)
            return false;

        _dbContext.Clinics.Remove(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("Clinic {ClinicId} PERMANENTLY DELETED from database", id);

        return true;
    }

    #endregion

    #region STATISTICS

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbContext.Clinics
            .AsNoTracking()
            .CountAsync(c => !c.IsDeleted);
    }

    public async Task<int> GetCountByStatusAsync(SubscriptionStatus status)
    {
        return await _dbContext.Clinics
            .AsNoTracking()
            .CountAsync(c => !c.IsDeleted && c.Status == status);
    }

    public async Task<int> GetActiveCountAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Clinics
            .AsNoTracking()
            .CountAsync(c => !c.IsDeleted && 
                c.Status == SubscriptionStatus.Active &&
                c.Subscriptions.Any(s => 
                    !s.IsDeleted && 
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > now));
    }

    #endregion
}