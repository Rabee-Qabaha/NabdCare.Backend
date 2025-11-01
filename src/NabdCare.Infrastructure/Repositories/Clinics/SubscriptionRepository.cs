using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Clinics;

/// <summary>
/// Repository for handling CRUD and pagination logic of clinic subscriptions.
/// Supports pagination, filtering, ABAC-based query restriction, and optional payment inclusion.
/// </summary>
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

    public async Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<PaginatedResult<Subscription>> GetByClinicIdPagedAsync(
        Guid clinicId,
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.ClinicId == clinicId && !s.IsDeleted)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        if (abacFilter is not null)
            query = abacFilter(query);

        return await ApplyPaginationAsync(query, pagination);
    }

    public async Task<PaginatedResult<Subscription>> GetAllPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        if (abacFilter is not null)
            query = abacFilter(query);

        return await ApplyPaginationAsync(query, pagination);
    }

    public async Task<PaginatedResult<Subscription>> GetPagedAsync(
        PaginationRequestDto pagination,
        bool includePayments = false,
        Func<IQueryable<Subscription>, IQueryable<Subscription>>? abacFilter = null)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        if (abacFilter is not null)
            query = abacFilter(query);

        return await ApplyPaginationAsync(query, pagination);
    }

    /// <summary>
    /// Get all subscriptions eligible for auto-renewal within their grace window.
    /// </summary>
    public async Task<List<Subscription>> GetAutoRenewCandidatesAsync(DateTime nowUtc)
    {
        return await _db.Subscriptions
            .AsNoTracking()
            .Where(s =>
                !s.IsDeleted &&
                s.AutoRenew &&
                (s.Status == SubscriptionStatus.Active ||
                 s.Status == SubscriptionStatus.Expired ||
                 s.Status == SubscriptionStatus.Trial) &&
                s.EndDate <= nowUtc &&
                s.EndDate.AddDays(s.GracePeriodDays) >= nowUtc)
            .ToListAsync();
    }

    #endregion

    #region COMMAND METHODS

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        await _db.Subscriptions.AddAsync(subscription);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Subscription {SubscriptionId} created", subscription.Id);
        return subscription;
    }

    public async Task<Subscription> UpdateAsync(Subscription subscription)
    {
        _db.Subscriptions.Update(subscription);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Subscription {SubscriptionId} updated", subscription.Id);
        return subscription;
    }

    public async Task<Subscription> UpdateStatusAsync(Subscription subscription)
    {
        var existing = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscription.Id);
        if (existing == null)
            throw new KeyNotFoundException($"Subscription {subscription.Id} not found.");

        existing.Status = subscription.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        _db.Subscriptions.Update(existing);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Subscription {SubscriptionId} status changed to {Status}.",
            existing.Id, existing.Status);
        return existing;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
        if (subscription == null) return false;

        subscription.IsDeleted = true;
        subscription.UpdatedAt = DateTime.UtcNow;

        _db.Subscriptions.Update(subscription);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Subscription {SubscriptionId} soft deleted", id);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
        if (subscription == null) return false;

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();

        _logger.LogWarning("Subscription {SubscriptionId} permanently deleted", id);
        return true;
    }

    #endregion

    #region PRIVATE HELPERS

    private static async Task<PaginatedResult<Subscription>> ApplyPaginationAsync(
        IQueryable<Subscription> query,
        PaginationRequestDto pagination)
    {
        var totalCount = await query.CountAsync();

        var limit = pagination.Limit <= 0 ? 20 : pagination.Limit > 100 ? 100 : pagination.Limit;

        // dynamic sorting if provided
        if (!string.IsNullOrEmpty(pagination.SortBy))
        {
            query = pagination.Descending
                ? query.OrderByDescending(e => EF.Property<object>(e, pagination.SortBy))
                : query.OrderBy(e => EF.Property<object>(e, pagination.SortBy));
        }

        // cursor-based pagination
        if (!string.IsNullOrEmpty(pagination.Cursor) &&
            Guid.TryParse(pagination.Cursor, out var cursorId))
        {
            query = query.Where(s => s.Id.CompareTo(cursorId) < 0);
        }

        var items = await query
            .Take(limit + 1)
            .AsNoTracking()
            .ToListAsync();

        bool hasMore = items.Count > limit;
        if (hasMore)
            items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore ? items.Last().Id.ToString() : null;

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