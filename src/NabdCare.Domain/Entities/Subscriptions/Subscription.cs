using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NabdCare.Domain.Entities.Subscriptions;

public class Subscription : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    [Required, MaxLength(50)]
    public string PlanId { get; set; } = string.Empty;

    // ✅ 2025 Best Practice: External Reference
    // Stores the Stripe/PayPal Subscription ID for future syncing
    [MaxLength(100)]
    public string? ExternalSubscriptionId { get; set; }

    // =================================================
    // 📅 LIFECYCLE & DATES
    // =================================================
    public DateTime StartDate { get; set; }
    
    // The date the subscription *actually* stops providing access
    public DateTime EndDate { get; set; } 

    // ✅ 2025 Best Practice: Billing Anchor
    // Keeps renewal dates stable even if payments fail/retry for a few days
    public DateTime? BillingCycleAnchor { get; set; }

    // ✅ 2025 Best Practice: Explicit Trial Logic
    // Separates "Trialing" status from actual billing start
    public DateTime? TrialEndsAt { get; set; }

    // =================================================
    // 🚫 CANCELLATION & CHURN LOGIC
    // =================================================
    
    // ✅ 2025 Best Practice: "Graceful Cancellation"
    // If true, the sub is "Active" but will die at the end of the period.
    // This allows you to show "Win-back" offers before they leave.
    public bool CancelAtPeriodEnd { get; set; } = false;
    
    public DateTime? CanceledAt { get; set; }
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // =================================================
    // 💰 FINANCIALS
    // =================================================
    
    public SubscriptionType Type { get; set; } // Monthly/Yearly
    
    // ✅ 2025 Best Practice: Multi-Currency Support
    // Never store amounts without their currency
    [Required]
    public Currency Currency { get; set; } = Currency.USD; 

    [Column(TypeName = "decimal(18,2)")]
    public decimal Fee { get; set; }

    public SubscriptionStatus Status { get; set; }

    // =================================================
    // 🌳 LIMITS (Your Snapshot Logic is Perfect)
    // =================================================
    public int PurchasedBranches { get; set; } = 0;
    public int IncludedBranchesSnapshot { get; set; } = 1;
    public int BonusBranches { get; set; } = 0;
    [NotMapped]
    public int MaxBranches => IncludedBranchesSnapshot + PurchasedBranches + BonusBranches;

    public int PurchasedUsers { get; set; } = 0;
    public int IncludedUsersSnapshot { get; set; } = 1;
    public int BonusUsers { get; set; } = 0;
    [NotMapped]
    public int MaxUsers => IncludedUsersSnapshot + PurchasedUsers + BonusUsers;

    // =================================================
    
    public bool AutoRenew { get; set; } = true; // Default to true for SaaS
    public int GracePeriodDays { get; set; } = 7;
    
    public string? Notes { get; set; }

    // 🔗 Relations
    public Guid? PreviousSubscriptionId { get; set; }
    public Subscription? PreviousSubscription { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}