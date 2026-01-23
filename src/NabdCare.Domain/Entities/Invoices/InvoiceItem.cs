using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Inventory; 
using NabdCare.Domain.Enums.Invoice;

namespace NabdCare.Domain.Entities.Invoices;

[Table("InvoiceItems")]
public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Note { get; set; }

    public InvoiceItemType Type { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}