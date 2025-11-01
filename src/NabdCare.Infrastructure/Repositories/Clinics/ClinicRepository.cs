using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;
using NabdCare.Infrastructure.Utils;

namespace NabdCare.Infrastructure.Repositories.Clinics;

/// <summary>
/// Production-ready clinic repository with cursor-based pagination,
/// sorting, and filtering support. Thin data access layer - no business logic.
/// Includes optional ABAC filter for security-aware querying.
/// </summary>
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
        if (id == Guid.Empty)
            return null;

        return await _dbContext.Clinics
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<PaginatedResult<Clinic>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var query = BaseClinicQuery();

        // ✅ Apply ABAC filter if provided
        if (abacFilter is not null)
            query = abacFilter(query);

        query = ApplyFilterAndSorting(query, pagination);
        return await PaginateAsync(query, pagination);
    }

    public async Task<PaginatedResult<Clinic>> GetByStatusPagedAsync(
        SubscriptionStatus status,
        PaginationRequestDto pagination,
        Func<IQueryable<Clinic>, IQueryable<Clinic>>? abacFilter = null)
    {
        var query = BaseClinicQuery().Where(c => c.Status == status);

        if (abacFilter is not null)
            query = abacFilter(query);

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

        if (abacFilter is not null)
            query = abacFilter(query);

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

        if (abacFilter is not null)
            query = abacFilter(query);

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

        if (abacFilter is not null)
            query = abacFilter(query);

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
            return new PaginatedResult<Clinic>
            {
                Items = Enumerable.Empty<Clinic>(),
                TotalCount = 0,
                HasMore = false
            };

        var search = query.Trim().ToLower();

        var dbQuery = BaseClinicQuery()
            .Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));

        if (abacFilter is not null)
            dbQuery = abacFilter(dbQuery);

        dbQuery = ApplyFilterAndSorting(dbQuery, pagination);
        return await PaginateAsync(dbQuery, pagination);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name.Trim().ToLower();
        var query = _dbContext.Clinics.Where(c => !c.IsDeleted && c.Name.ToLower() == normalized);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalized = email.Trim().ToLower();
        var query = _dbContext.Clinics.Where(c => !c.IsDeleted && c.Email != null && c.Email.ToLower() == normalized);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

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

        _dbContext.Clinics.Update(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Clinic {ClinicId} soft deleted", id);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var clinic = await _dbContext.Clinics.FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return false;

        _dbContext.Clinics.Remove(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("Clinic {ClinicId} permanently deleted", id);
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

    private IQueryable<Clinic> BaseClinicQuery()
    {
        return _dbContext.Clinics
            .AsNoTracking()
            .Include(c => c.Subscriptions.Where(s => !s.IsDeleted))
            .Where(c => !c.IsDeleted);
    }

    private static IQueryable<Clinic> ApplyFilterAndSorting(IQueryable<Clinic> query, PaginationRequestDto pagination)
    {
        // ✅ Apply text filter if provided
        if (!string.IsNullOrWhiteSpace(pagination.Filter))
        {
            var filter = pagination.Filter.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(filter) ||
                (c.Email != null && c.Email.ToLower().Contains(filter)) ||
                (c.Phone != null && c.Phone.Contains(filter)));
        }

        // ✅ Dynamic sorting support
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