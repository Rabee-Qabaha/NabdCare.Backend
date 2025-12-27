using NabdCare.Domain.Enums.Invoice;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Invoices;

[ExportTsClass]
public class GenerateInvoiceRequestDto
{
    public Guid ClinicId { get; set; }
    public Guid SubscriptionId { get; set; }
    public InvoiceType Type { get; set; }
    
    public string Currency { get; set; } = "USD";

    public string? IdempotencyKey { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    
    public List<GenerateInvoiceItemDto> Items { get; set; } = new();
    public decimal TaxRate { get; set; } = 0;
}