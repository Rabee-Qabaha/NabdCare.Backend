using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums.Invoice;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NabdCare.Domain.Entities.Invoices;

[Table("Invoices")]
public class Invoice : BaseEntity
{
    // ✅ 2025 Best Practice: Human-Readable Sequential IDs
    // e.g., "INV-2025-001" instead of a Guid
    [Required, MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    // ✅ 2025 Best Practice: Idempotency Key
    // Prevents double-billing if the server crashes during generation
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    // ==========================================
    // 📸 IMMUTABLE SNAPSHOT
    // ==========================================
    [Required, MaxLength(255)]
    public string BilledToName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? BilledToAddress { get; set; }
    [MaxLength(50)]
    public string? BilledToTaxNumber { get; set; }

    // ==========================================
    // 📂 DELIVERABLES
    // ==========================================
    
    // ✅ 2025 Best Practice: Blob Storage URL
    // Never generate PDFs on the fly for old invoices; store the static file.
    [MaxLength(500)]
    public string? PdfUrl { get; set; }
    
    // ✅ 2025 Best Practice: Hosted Payment Page
    // Link to send to customer (e.g. Stripe Hosted Invoice Page)
    [MaxLength(500)]
    public string? HostedPaymentUrl { get; set; }

    // ==========================================
    // 📅 DATES & STATUS
    // ==========================================
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    
    // For Dunning (Retry logic)
    public int PaymentAttemptCount { get; set; } = 0;
    public DateTime? NextPaymentAttempt { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceType Type { get; set; }

    // ==========================================
    // 💰 FINANCIALS
    // ==========================================
    
    // ✅ Currency is mandatory on the Invoice itself
    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxRate { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0;

    [NotMapped]
    public decimal BalanceDue => TotalAmount - PaidAmount;

    // 🔗 Relations
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}