using Microsoft.EntityFrameworkCore;
using NabdCare.Application.DTOs.Reports;
using NabdCare.Application.Interfaces.Reports;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Reports;

public class ReportRepository : IReportRepository
{
    private readonly NabdCareDbContext _context;

    public ReportRepository(NabdCareDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetOpeningBalanceAsync(Guid clinicId, DateTime start)
    {
        var invoicesBefore = await _context.Invoices
            .Where(i => i.ClinicId == clinicId && i.IssueDate < start && !i.IsDeleted)
            .SumAsync(i => i.TotalAmount);

        var paymentsBefore = await _context.Payments
            .Where(p => p.ClinicId == clinicId && p.PaymentDate < start && !p.IsDeleted)
            .SumAsync(p => p.Amount - p.RefundedAmount);

        return invoicesBefore - paymentsBefore;
    }

    public async Task<List<StatementLineItemDto>> GetInvoiceTransactionsAsync(Guid clinicId, DateTime start, DateTime end)
    {
        return await _context.Invoices
            .Where(i => i.ClinicId == clinicId && i.IssueDate >= start && i.IssueDate <= end && !i.IsDeleted)
            .Select(i => new StatementLineItemDto
            {
                Date = i.IssueDate,
                Type = "Invoice",
                Reference = i.InvoiceNumber,
                Description = $"Invoice #{i.InvoiceNumber}",
                Debit = i.TotalAmount,
                Credit = 0
            })
            .ToListAsync();
    }

    public async Task<List<StatementLineItemDto>> GetPaymentTransactionsAsync(Guid clinicId, DateTime start, DateTime end)
    {
        return await _context.Payments
            .Where(p => p.ClinicId == clinicId && p.PaymentDate >= start && p.PaymentDate <= end && !p.IsDeleted)
            .Select(p => new StatementLineItemDto
            {
                Date = p.PaymentDate,
                Type = "Payment",
                Reference = p.TransactionId ?? "CASH",
                Description = $"Payment via {p.Method}",
                Debit = 0,
                Credit = p.Amount - p.RefundedAmount
            })
            .ToListAsync();
    }

    public async Task<dynamic?> GetPaymentReceiptDataAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Clinic)
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Invoice)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null) return null;

        return new
        {
            ReceiptNumber = $"RCPT-{payment.Id.ToString().Substring(0, 8).ToUpper()}",
            Date = payment.PaymentDate,
            AmountPaid = payment.Amount,
            Method = payment.Method.ToString(),
            Payer = payment.Clinic?.Name ?? "Unknown",
            Allocations = payment.Allocations.Select(a => new 
            {
                InvoiceNumber = a.Invoice.InvoiceNumber,
                Amount = a.Amount
            }),
            UnallocatedCredit = payment.UnallocatedAmount
        };
    }
}