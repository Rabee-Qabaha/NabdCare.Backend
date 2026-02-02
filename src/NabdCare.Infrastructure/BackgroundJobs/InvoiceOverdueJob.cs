using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Domain.Enums.Invoice;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.BackgroundJobs;

public class InvoiceOverdueJob
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<InvoiceOverdueJob> _logger;

    public InvoiceOverdueJob(NabdCareDbContext dbContext, ILogger<InvoiceOverdueJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting Invoice Overdue Check...");

        var today = DateTime.UtcNow.Date;

        // Find invoices that are:
        // 1. Not Paid (Issued, Sent, PartiallyPaid)
        // 2. Due Date has passed
        // 3. Not already marked Overdue
        var overdueInvoices = await _dbContext.Invoices
            .Where(i => i.Status != InvoiceStatus.Paid 
                        && i.Status != InvoiceStatus.Void 
                        && i.Status != InvoiceStatus.Overdue
                        && i.DueDate < today)
            .ToListAsync();

        if (overdueInvoices.Any())
        {
            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedAt = DateTime.UtcNow;
                invoice.UpdatedBy = "System-Job";
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Marked {Count} invoices as Overdue.", overdueInvoices.Count);
        }
        else
        {
            _logger.LogInformation("No new overdue invoices found.");
        }
    }
}