using Microsoft.EntityFrameworkCore;
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