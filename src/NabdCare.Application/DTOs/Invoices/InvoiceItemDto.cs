using NabdCare.Domain.Enums.Invoice;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Invoices;

[ExportTsClass]
public class InvoiceItemDto
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Description of the charge (e.g. "Standard Yearly Plan")
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Contextual note (e.g. "Includes 5 Users, 1 Branch")
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Type of charge: BasePlan, AddonUser, BonusItem, etc.
    /// </summary>
    public InvoiceItemType Type { get; set; }

    // Financial Snapshot
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }

    // Proration Context (Optional)
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}