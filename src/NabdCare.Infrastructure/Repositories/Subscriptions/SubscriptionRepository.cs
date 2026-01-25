using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Subscriptions;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Subscriptions;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly NabdCareDbContext _db;
    private readonly ILogger<SubscriptionRepository> _logger;

    public SubscriptionRepository(NabdCareDbContext db, ILogger<SubscriptionRepository> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

    public async Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false, bool includeInvoices = false)
    {
        var query = BaseQuery().Where(s => s.Id == id);
        if (includePayments) query = query.Include(s => s.Payments);
        if (includeInvoices) query = query.Include(s => s.Invoices);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<Subscription?> GetActiveByClinicIdAsync(Guid clinicId)
    {
        var now = DateTime.UtcNow;
        return await BaseQuery()
            .Include(s => s.Invoices)
            .Where(s => s.ClinicId == clinicId 
                        && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.PastDue)
                        && s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
    }
    
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _db.Database.BeginTransactionAsync();
    }

    public async Task<List<Subscription>> GetAutoRenewCandidatesAsync(DateTime nowUtc)
    {
        return await BaseQuery()
            .Where(s => s.AutoRenew 
                        && !s.CancelAtPeriodEnd
                        && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial || s.Status == SubscriptionStatus.PastDue)
                        && s.EndDate <= nowUtc
                        && s.EndDate.AddDays(s.GracePeriodDays) >= nowUtc)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetCancellationCandidatesAsync(DateTime nowUtc)
    {
        return await BaseQuery()
            .Where(s => s.CancelAtPeriodEnd 
                        && s.Status != SubscriptionStatus.Cancelled 
                        && s.Status != SubscriptionStatus.Expired
                        && s.EndDate <= nowUtc)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetExpiredCandidatesAsync(DateTime nowUtc)
    {
        return await BaseQuery()
            .Where(s => s.Status == SubscriptionStatus.Active 
                        && !s.AutoRenew
                        && s.EndDate.AddDays(s.GracePeriodDays) <= nowUtc)
            .ToListAsync();
    }

    public async Task<bool> HasFutureSubscriptionAsync(Guid clinicId, DateTime afterDate)
    {
        return await BaseQuery()
            .AnyAsync(s => s.ClinicId == clinicId && s.Status == SubscriptionStatus.Future && s.StartDate >= afterDate);
    }

    public async Task<List<Subscription>> GetFutureSubscriptionsStartingByAsync(DateTime date)
    {
        return await BaseQuery()
            .Where(s => s.Status == SubscriptionStatus.Future && s.StartDate <= date)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetFutureSubscriptionsByClinicAsync(Guid clinicId)
    {
        return await BaseQuery()
            .Where(s => s.ClinicId == clinicId && s.Status == SubscriptionStatus.Future)
            .ToListAsync();
    }

    public async Task<PaginatedResult<Subscription>> GetByClinicIdPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        var query = BaseQuery().Where(s => s.ClinicId == clinicId);
        if (includePayments) query = query.Include(s => s.Payments);
        if (abacFilter != null) query = abacFilter(query);
        return await ApplyCursorPaginationAsync(query, pagination);
    }

    public async Task<PaginatedResult<Subscription>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        var query = BaseQuery();
        if (includePayments) query = query.Include(s => s.Payments);
        if (abacFilter != null) query = abacFilter(query);
        return await ApplyCursorPaginationAsync(query, pagination);
    }

    public async Task<PaginatedResult<Subscription>> GetPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        return await GetAllPagedAsync(pagination, includePayments, abacFilter);
    }

    #endregion

    #region COMMAND METHODS

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

    public async Task<bool> UpdateStatusAsync(Subscription subscription)
    {
        var entry = _db.Subscriptions.Attach(subscription);
        entry.Property(x => x.Status).IsModified = true;
        entry.Property(x => x.UpdatedAt).IsModified = true;
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
        if (sub == null) return false;
        _db.Subscriptions.Remove(sub);
        return await _db.SaveChangesAsync() > 0;
    }

    #endregion

    #region HELPERS

    private IQueryable<Subscription> BaseQuery() => _db.Subscriptions.AsNoTracking().Where(s => !s.IsDeleted);

    /// <summary>
    /// Implements generic cursor pagination based on CreatedAt ticks.
    /// Supports forward navigation only for simplicity and consistency.
    /// </summary>
    private async Task<PaginatedResult<Subscription>> ApplyCursorPaginationAsync(
        IQueryable<Subscription> query, 
        PaginationRequestDto pagination)
    {
        // 1. Default Sorting (Cursor pagination relies on stable sorting)
        // We use CreatedAt desc as the primary sort key.
        query = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);

        // 2. Apply Cursor Filter
        if (!string.IsNullOrWhiteSpace(pagination.Cursor))
        {
            if (long.TryParse(pagination.Cursor, out long cursorTicks))
            {
                var cursorDate = new DateTime(cursorTicks, DateTimeKind.Utc);
                // Fetch items older than the cursor (since we sort descending)
                query = query.Where(x => x.CreatedAt < cursorDate);
            }
        }

        // 3. Limit (Clamp between 1 and 100)
        var limit = Math.Clamp(pagination.Limit, 1, 100);

        // 4. Fetch Data + 1 (Peek Strategy)
        var items = await query.Take(limit + 1).ToListAsync();

        // 5. Determine Next Cursor
        bool hasMore = items.Count > limit;
        string? nextCursor = null;

        if (hasMore)
        {
            // Remove the extra item used for checking availability
            var lastItem = items[limit - 1]; 
            items.RemoveAt(limit);
            
            // Cursor is the CreatedAt ticks of the last item in the page
            nextCursor = lastItem.CreatedAt.Ticks.ToString();
        }

        // 6. Total Count (Optional but requested)
        var totalCount = await _db.Subscriptions.CountAsync(s => !s.IsDeleted);

        return new PaginatedResult<Subscription>
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    #endregion
}