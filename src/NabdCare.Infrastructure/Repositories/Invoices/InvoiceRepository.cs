using Microsoft.EntityFrameworkCore;
using NabdCare.Application.DTOs.Invoices;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Invoices;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Invoices;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly NabdCareDbContext _dbContext;

    public InvoiceRepository(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.Clinic)
            .Include(i => i.PaymentAllocations) 
                .ThenInclude(pa => pa.Payment)
                    .ThenInclude(p => p.ChequeDetail) // ✅ Added: Include ChequeDetail
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice?> GetByNumberAsync(string invoiceNumber)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.PaymentAllocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<PaginatedResult<Invoice>> GetPagedAsync(
        InvoiceListRequestDto filter,
        Func<IQueryable<Invoice>, IQueryable<Invoice>>? abacFilter = null)
    {
        var query = _dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.PaymentAllocations) 
                .ThenInclude(pa => pa.Payment)
                    .ThenInclude(p => p.ChequeDetail)
            .AsNoTracking()
            .AsQueryable();

        // 1) ABAC
        if (abacFilter != null) query = abacFilter(query);

        // 2) Filters
        if (filter.ClinicId.HasValue)
            query = query.Where(i => i.ClinicId == filter.ClinicId.Value);

        if (filter.SubscriptionId.HasValue)
            query = query.Where(i => i.SubscriptionId == filter.SubscriptionId.Value);

        if (!string.IsNullOrWhiteSpace(filter.InvoiceNumber))
            query = query.Where(i => i.InvoiceNumber.Contains(filter.InvoiceNumber));

        if (filter.Status.HasValue)
            query = query.Where(i => i.Status == filter.Status.Value);

        if (filter.Type.HasValue)
            query = query.Where(i => i.Type == filter.Type.Value);
            
        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query = query.Where(i => i.Currency == filter.Currency);

        // 3) Sorting & Pagination (Cursor)
        query = query.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id);

        var limit = Math.Clamp(filter.Limit, 1, 100);

        if (!string.IsNullOrWhiteSpace(filter.Cursor))
        {
            var parts = filter.Cursor.Split('_', 2);
            if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParse(parts[1], out var cursorId))
            {
                var cursorDate = new DateTime(ticks, DateTimeKind.Utc);
                query = query.Where(i => i.IssueDate < cursorDate || (i.IssueDate == cursorDate && i.Id < cursorId));
            }
        }

        var pageItems = await query.Take(limit + 1).ToListAsync();
        var hasMore = pageItems.Count > limit;
        var items = pageItems.Take(limit).ToList();

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items.Last();
            nextCursor = $"{last.IssueDate.Ticks}_{last.Id}";
        }

        var totalCount = await query.CountAsync();

        return new PaginatedResult<Invoice>
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        if (!string.IsNullOrEmpty(invoice.IdempotencyKey))
        {
            var existing = await _dbContext.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.IdempotencyKey == invoice.IdempotencyKey);
            
            if (existing != null) return existing;
        }

        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice> UpdateAsync(Invoice invoice)
    {
        _dbContext.Invoices.Update(invoice);
        await _dbContext.SaveChangesAsync();
        return invoice;
    }

    public async Task<string> GenerateNextInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        var lastInvoice = await _dbContext.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var currentSeq))
            {
                nextSeq = currentSeq + 1;
            }
        }

        return $"{prefix}{nextSeq:D5}";
    }
}