using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Utils;

namespace NabdCare.Infrastructure.Repositories.Clinics;

public class ClinicRepository : IClinicRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<ClinicRepository> _logger;

    public ClinicRepository(NabdCareDbContext dbContext, ILogger<ClinicRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

    public async Task<Clinic?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty) return null;

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Clinic?> GetEntityByIdAsync(Guid id)
    {
        return await _dbContext.Clinics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<PaginatedResult<Clinic>> GetAllPagedAsync(
        ClinicFilterRequestDto filters,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var query = BaseClinicQuery(filters.IncludeDeleted);

        if (abacFilter is not null)
            query = abacFilter(query);

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                c.Slug.Contains(term)
            );
        }

        if (!string.IsNullOrWhiteSpace(filters.Name))
            query = query.Where(c => c.Name.ToLower().Contains(filters.Name.ToLower()));

        if (!string.IsNullOrWhiteSpace(filters.Email))
            query = query.Where(c => c.Email != null && c.Email.ToLower().Contains(filters.Email.ToLower()));

        if (!string.IsNullOrWhiteSpace(filters.Phone))
            query = query.Where(c => c.Phone != null && c.Phone.Contains(filters.Phone));

        if (filters.Status.HasValue)
            query = query.Where(c => c.Status == filters.Status.Value);

        if (filters.SubscriptionType.HasValue)
        {
            query = query.Where(c => c.Subscriptions.Any(s => 
                !s.IsDeleted && 
                s.Status == SubscriptionStatus.Active && 
                s.Type == filters.SubscriptionType.Value));
        }

        if (filters.SubscriptionFee.HasValue)
        {
            query = query.Where(c => c.Subscriptions.Any(s => 
                !s.IsDeleted && 
                s.Status == SubscriptionStatus.Active && 
                s.Fee >= filters.SubscriptionFee.Value));
        }

        if (filters.CreatedAt.HasValue)
        {
            var date = filters.CreatedAt.Value.Date;
            query = query.Where(c => c.CreatedAt >= date);
        }

        query = ApplyFilterAndSorting(query, filters);
        return await PaginateAsync(query, filters);
    }

    public async Task<PaginatedResult<Clinic>> GetByStatusPagedAsync(
        SubscriptionStatus status,
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var query = BaseClinicQuery().Where(c => c.Status == status);

        if (abacFilter is not null) query = abacFilter(query);

        query = ApplyFilterAndSorting(query, pagination);
        return await PaginateAsync(query, pagination);
    }

    public async Task<PaginatedResult<Clinic>> GetActiveWithValidSubscriptionPagedAsync(
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var now = DateTime.UtcNow;
        var query = BaseClinicQuery()
            .Where(c =>
                c.Status == SubscriptionStatus.Active &&
                c.Subscriptions.Any(s =>
                    !s.IsDeleted &&
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > now));

        if (abacFilter is not null) query = abacFilter(query);

        query = ApplyFilterAndSorting(query, pagination);
        return await PaginateAsync(query, pagination);
    }

    public async Task<PaginatedResult<Clinic>> GetWithExpiringSubscriptionsPagedAsync(
        int withinDays,
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var now = DateTime.UtcNow;
        var expirationDate = now.AddDays(withinDays);

        var query = BaseClinicQuery()
            .Where(c =>
                c.Status == SubscriptionStatus.Active &&
                c.Subscriptions.Any(s =>
                    !s.IsDeleted &&
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate > now &&
                    s.EndDate <= expirationDate));

        if (abacFilter is not null) query = abacFilter(query);

        query = ApplyFilterAndSorting(query, pagination);
        return await PaginateAsync(query, pagination);
    }

    public async Task<PaginatedResult<Clinic>> GetWithExpiredSubscriptionsPagedAsync(
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var now = DateTime.UtcNow;
        var query = BaseClinicQuery()
            .Where(c =>
                c.Subscriptions.Any(s =>
                    !s.IsDeleted &&
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate <= now));

        if (abacFilter is not null) query = abacFilter(query);

        query = ApplyFilterAndSorting(query, pagination);
        return await PaginateAsync(query, pagination);
    }

    public async Task<IEnumerable<Clinic>> GetWithExpiredSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c =>
                !c.IsDeleted &&
                c.Subscriptions.Any(s =>
                    !s.IsDeleted &&
                    s.Status == SubscriptionStatus.Active &&
                    s.EndDate <= now))
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PaginatedResult<Clinic>> SearchPagedAsync(
        string query,
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new PaginatedResult<Clinic> { Items = [], TotalCount = 0, HasMore = false };

        var search = query.Trim().ToLower();
        var dbQuery = BaseClinicQuery()
            .Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));

        if (abacFilter is not null) dbQuery = abacFilter(dbQuery);

        dbQuery = ApplyFilterAndSorting(dbQuery, pagination);
        return await PaginateAsync(dbQuery, pagination);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var normalized = name.Trim().ToLower();
        var query = _dbContext.Clinics.Where(c => !c.IsDeleted && c.Name.ToLower() == normalized);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var normalized = email.Trim().ToLower();
        var query = _dbContext.Clinics.Where(c => !c.IsDeleted && c.Email != null && c.Email.ToLower() == normalized);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }
    
    public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        var normalized = slug.Trim().ToLower();
        var query = _dbContext.Clinics.Where(c => c.Slug == normalized); 
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
        => id != Guid.Empty && await _dbContext.Clinics.AnyAsync(c => c.Id == id && !c.IsDeleted);

    #endregion

    #region COMMAND METHODS

    public async Task<Clinic> CreateAsync(Clinic clinic)
    {
        await _dbContext.Clinics.AddAsync(clinic);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Clinic {ClinicId} created", clinic.Id);
        return clinic;
    }

    public async Task<Clinic> UpdateAsync(Clinic clinic)
    {
        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Clinic {ClinicId} updated", clinic.Id);
        return clinic;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (clinic == null) return false;

        clinic.IsDeleted = true;
        clinic.DeletedAt = DateTime.UtcNow;
        clinic.Status = SubscriptionStatus.Suspended;

        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Clinic {ClinicId} soft deleted and suspended", id);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var clinic = await _dbContext.Clinics.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return false;

        _dbContext.Clinics.Remove(clinic);
        await _dbContext.SaveChangesAsync();
        _logger.LogWarning("Clinic {ClinicId} permanently deleted", id);
        return true;
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var clinic = await _dbContext.Clinics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);

        if (clinic == null) return false;

        clinic.IsDeleted = false;
        clinic.DeletedAt = null;
        clinic.DeletedBy = null;
        clinic.Status = SubscriptionStatus.Suspended; 

        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    #endregion

    #region STATISTICS

    public async Task<int> GetTotalCountAsync() =>
        await _dbContext.Clinics.CountAsync(c => !c.IsDeleted);

    public async Task<int> GetCountByStatusAsync(SubscriptionStatus status) =>
        await _dbContext.Clinics.CountAsync(c => !c.IsDeleted && c.Status == status);

    public async Task<int> GetActiveCountAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbContext.Clinics.CountAsync(c =>
            !c.IsDeleted &&
            c.Status == SubscriptionStatus.Active &&
            c.Subscriptions.Any(s => !s.IsDeleted && s.Status == SubscriptionStatus.Active && s.EndDate > now));
    }

    #endregion

    #region PRIVATE HELPERS

    private IQueryable<Clinic> BaseClinicQuery(bool includeDeleted = false)
    {
        IQueryable<Clinic> query = _dbContext.Clinics
            .AsNoTracking()
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted));

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters().Where(c => c.IsDeleted);
        }
        else
        {
            query = query.Where(c => !c.IsDeleted);
        }
        
        return query;
    }

    private static IQueryable<Clinic> ApplyFilterAndSorting(IQueryable<Clinic> query, PaginationRequestDto pagination)
    {
        if (!string.IsNullOrWhiteSpace(pagination.Filter))
        {
            var filter = pagination.Filter.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(filter) ||
                (c.Email != null && c.Email.ToLower().Contains(filter)) ||
                (c.Phone != null && c.Phone.Contains(filter)));
        }

        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            query = pagination.Descending
                ? query.OrderByDescendingDynamic(pagination.SortBy)
                : query.OrderByDynamic(pagination.SortBy);
        }
        else
        {
            query = query.OrderBy(c => c.Name);
        }

        return query;
    }

    private async Task<PaginatedResult<Clinic>> PaginateAsync(IQueryable<Clinic> query, PaginationRequestDto pagination)
    {
        var limit = pagination.Limit <= 0 ? 20 : pagination.Limit > 100 ? 100 : pagination.Limit;
        Guid? cursorGuid = null;

        if (!string.IsNullOrEmpty(pagination.Cursor) && Guid.TryParse(pagination.Cursor, out var parsed))
            cursorGuid = parsed;
        
        if (cursorGuid.HasValue)
            query = query.Where(c => c.Id.CompareTo(cursorGuid.Value) > 0);

        var items = await query.Take(limit + 1).AsNoTracking().ToListAsync();
        bool hasMore = items.Count > limit;

        if (hasMore) items.RemoveAt(items.Count - 1);
        var nextCursor = hasMore ? items.Last().Id.ToString() : null;
        var totalCount = await query.CountAsync();

        return new PaginatedResult<Clinic>
        {
            Items = items,
            HasMore = hasMore,
            NextCursor = nextCursor,
            TotalCount = totalCount
        };
    }

    #endregion
}