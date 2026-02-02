using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Domain.Entities.Payments;

[Table("PaymentAllocations")]
public class PaymentAllocation : BaseEntity
{
    public Guid PaymentId { get; set; }
    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // Optional: Date this allocation was made (might differ from Payment Date)
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
}