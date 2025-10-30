using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Clinics;

/// <summary>
/// Repository for handling CRUD and pagination logic of clinic subscriptions.
/// Supports pagination, filtering, and optional payment inclusion.
/// </summary>
public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly NabdCareDbContext _db;

    public SubscriptionRepository(NabdCareDbContext db)
    {
        _db = db;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions.AsNoTracking();

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await query.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<PaginatedResult<Subscription>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto pagination, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .Where(s => s.ClinicId == clinicId && !s.IsDeleted)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await ApplyPaginationAsync(query, pagination);
    }

    public async Task<PaginatedResult<Subscription>> GetAllPagedAsync(PaginationRequestDto pagination, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await ApplyPaginationAsync(query, pagination);
    }

    public async Task<PaginatedResult<Subscription>> GetPagedAsync(PaginationRequestDto pagination, bool includePayments = false)
    {
        IQueryable<Subscription> query = _db.Subscriptions
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.StartDate);

        if (includePayments)
            query = query.Include(s => s.Payments);

        return await ApplyPaginationAsync(query, pagination);
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

    #region PRIVATE HELPERS

    private static async Task<PaginatedResult<Subscription>> ApplyPaginationAsync(IQueryable<Subscription> query, PaginationRequestDto pagination)
    {
        var totalCount = await query.CountAsync();

        if (pagination.Limit <= 0)
            pagination.Limit = 20;
        if (pagination.Limit > 100)
            pagination.Limit = 100;

        // Apply sorting
        if (!string.IsNullOrEmpty(pagination.SortBy))
        {
            // Simple dynamic sort by property name (safe for common use)
            query = pagination.Descending
                ? query.OrderByDescending(e => EF.Property<object>(e, pagination.SortBy))
                : query.OrderBy(e => EF.Property<object>(e, pagination.SortBy));
        }

        // Cursor-based pagination (optional)
        if (!string.IsNullOrEmpty(pagination.Cursor))
        {
            if (Guid.TryParse(pagination.Cursor, out var cursorId))
            {
                query = query.Where(s => s.Id.CompareTo(cursorId) < 0);
            }
        }

        var items = await query
            .Take(pagination.Limit + 1)
            .AsNoTracking()
            .ToListAsync();

        var hasMore = items.Count > pagination.Limit;
        var nextCursor = hasMore ? items.Last().Id.ToString() : null;

        if (hasMore)
            items.RemoveAt(items.Count - 1);

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