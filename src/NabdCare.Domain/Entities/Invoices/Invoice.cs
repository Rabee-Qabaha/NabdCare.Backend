using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Entities.Patients;
using NabdCare.Domain.Entities.Clinical;
using NabdCare.Domain.Enums.Invoice;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NabdCare.Domain.Entities.Invoices;

[Table("Invoices")]
public class Invoice : BaseEntity
{
    // ✅ IDENTITY
    [Required, MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    // ✅ TENANCY
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    // ==========================================
    // 🔗 POLYMORPHIC LINKS (The "Either/Or" Logic)
    // ==========================================
    
    // CASE A: Billing the Clinic (SaaS Fee)
    public Guid? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    // CASE B: Billing the Patient (Medical Service)
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    // Link to the Medical Visit (Optional)
    public Guid? ClinicalEncounterId { get; set; }
    public ClinicalEncounter? ClinicalEncounter { get; set; }

    // ==========================================
    // 📸 IMMUTABLE SNAPSHOT (Keep this!)
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
    [MaxLength(500)]
    public string? PdfUrl { get; set; }
    
    [MaxLength(500)]
    public string? HostedPaymentUrl { get; set; }

    // ==========================================
    // 📅 DATES & STATUS
    // ==========================================
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    
    public int PaymentAttemptCount { get; set; } = 0;
    public DateTime? NextPaymentAttempt { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    
    // Helps distinguish "SaaS Fee" vs "Patient Bill"
    public InvoiceType Type { get; set; } 

    // ==========================================
    // 💰 FINANCIALS
    // ==========================================
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
    
    // 🔗 Replaced direct Payments with Allocations
    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
}