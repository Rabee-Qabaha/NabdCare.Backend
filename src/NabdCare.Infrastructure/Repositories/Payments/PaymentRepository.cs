using Microsoft.EntityFrameworkCore;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Payments;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Payments;

public class PaymentRepository : IPaymentRepository
{
    private readonly NabdCareDbContext _context;

    public PaymentRepository(NabdCareDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(Guid paymentId, bool includeChequeDetails = false)
    {
        var query = _context.Payments
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Invoice)
            .AsQueryable();

        if (includeChequeDetails)
        {
            query = query.Include(p => p.ChequeDetail);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<IEnumerable<Payment>> GetAllAsync(bool includeChequeDetails = false)
    {
        var query = _context.Payments
            .Include(p => p.Allocations)
            .AsQueryable();

        if (includeChequeDetails)
        {
            query = query.Include(p => p.ChequeDetail);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByClinicIdAsync(Guid clinicId, bool includeChequeDetails = false)
    {
        var query = _context.Payments
            .Where(p => p.ClinicId == clinicId)
            .Include(p => p.Allocations)
            .AsQueryable();

        if (includeChequeDetails)
        {
            query = query.Include(p => p.ChequeDetail);
        }

        return await query.ToListAsync();
    }

    public async Task<PaginatedResult<Payment>> GetByClinicIdPagedAsync(Guid clinicId, PaginationRequestDto pagination, bool includeChequeDetails = false)
    {
        var query = _context.Payments
            .Where(p => p.ClinicId == clinicId)
            .Include(p => p.Allocations)
            .AsQueryable();

        if (includeChequeDetails)
        {
            query = query.Include(p => p.ChequeDetail);
        }

        // Sorting (Default: Newest First)
        query = query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id);

        // Cursor Pagination Logic
        var limit = Math.Clamp(pagination.Limit, 1, 100);

        if (!string.IsNullOrWhiteSpace(pagination.Cursor))
        {
            var parts = pagination.Cursor.Split('_', 2);
            if (parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParse(parts[1], out var cursorId))
            {
                var cursorDate = new DateTime(ticks, DateTimeKind.Utc);
                query = query.Where(p => p.PaymentDate < cursorDate || (p.PaymentDate == cursorDate && p.Id < cursorId));
            }
        }

        var pageItems = await query.Take(limit + 1).ToListAsync();
        var hasMore = pageItems.Count > limit;
        var items = pageItems.Take(limit).ToList();

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items.Last();
            nextCursor = $"{last.PaymentDate.Ticks}_{last.Id}";
        }

        var totalCount = await _context.Payments.CountAsync(p => p.ClinicId == clinicId);

        return new PaginatedResult<Payment>
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    public async Task<IEnumerable<Payment>> GetByPatientIdAsync(Guid patientId, bool includeChequeDetails = false)
    {
        var query = _context.Payments
            .Where(p => p.PatientId == patientId)
            .Include(p => p.Allocations)
            .AsQueryable();

        if (includeChequeDetails)
        {
            query = query.Include(p => p.ChequeDetail);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetPagedAsync(int page, int pageSize, bool includeChequeDetails = false)
    {
        var query = _context.Payments
            .Include(p => p.Allocations)
            .OrderByDescending(p => p.PaymentDate)
            .AsQueryable();

        if (includeChequeDetails)
        {
            query = query.Include(p => p.ChequeDetail);
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> SoftDeleteAsync(Guid paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null) return false;

        payment.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null) return false;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
        return true;
    }
}