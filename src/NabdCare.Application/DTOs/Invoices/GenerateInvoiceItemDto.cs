using NabdCare.Domain.Enums.Invoice;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Invoices;

[ExportTsClass]
public class GenerateInvoiceItemDto
{
    public string Description { get; set; } = string.Empty;
    public string? Note { get; set; }
    public InvoiceItemType Type { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}