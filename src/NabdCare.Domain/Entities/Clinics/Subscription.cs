using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinics;

/// <summary>
/// Represents a clinic subscription record.
/// Tracks full lifecycle (active, expired, suspended, etc.)
/// and links to previous renewals for audit/history.
/// </summary>
public class Subscription : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public SubscriptionType Type { get; set; }
    public decimal Fee { get; set; }
    public SubscriptionStatus Status { get; set; }

    // 🔗 For renewals / history tracking
    public Guid? PreviousSubscriptionId { get; set; }
    public Subscription? PreviousSubscription { get; set; }

    // 💳 Billing / invoicing support (future-proof)
    public Guid? PaymentId { get; set; }
    public string? InvoiceNumber { get; set; }

    // 🔁 Auto-renewal and grace period (optional)
    public bool AutoRenew { get; set; } = false;
    public int GracePeriodDays { get; set; } = 7;

    // 📝 Optional admin notes
    public string? Notes { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}