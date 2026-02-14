using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Patients;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Payments;

public class Payment : BaseEntity
{
    [Required]
    public PaymentContext Context { get; set; }

    // Foreign Keys (only one must be filled depending on Context)
    public Guid? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public Currency Currency { get; set; } = Currency.USD;

    [Required]
    [Column(TypeName = "decimal(18, 6)")]
    public decimal BaseExchangeRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 6)")]
    public decimal FinalExchangeRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal AmountInFunctionalCurrency { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundedAmount { get; set; } = 0;

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    public PaymentMethod Method { get; set; }

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // External Reference (Stripe ID, Bank Transfer ID, etc.)
    [MaxLength(100)]
    public string? TransactionId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation for cheque
    public ChequePaymentDetail? ChequeDetail { get; set; }

    // The Allocations (Where did this money go?)
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();

    // Computed: How much is left as "Credit"?
    // Logic: Total - Allocated - Refunded
    [NotMapped]
    public decimal UnallocatedAmount => Amount - RefundedAmount - (Allocations?.Sum(a => a.Amount) ?? 0);
}