using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Subscriptions;

[ExportTsClass]
public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    
    public string PlanId { get; set; } = string.Empty;
    public string? ExternalSubscriptionId { get; set; } // ✅ Stripe/PayPal ID
    
    // =================================================
    // 📅 LIFECYCLE & DATES
    // =================================================
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; } // ✅ Explicit Trial End
    public DateTime? BillingCycleAnchor { get; set; } // ✅ Recurring Billing Date
    
    public SubscriptionType Type { get; set; }
    
    // =================================================
    // 💰 FINANCIALS
    // =================================================
    public Currency Currency { get; set; } = Currency.USD; // ✅ 2025 Standard
    public decimal Fee { get; set; }
    public SubscriptionStatus Status { get; set; }

    // =================================================
    // 🚫 CHURN MANAGEMENT
    // =================================================
    public bool AutoRenew { get; set; }
    public bool CancelAtPeriodEnd { get; set; } // ✅ Churn Prevention Flag
    public string? CancellationReason { get; set; } // ✅ Analytics
    public int GracePeriodDays { get; set; }

    // =================================================
    // 🌳 BRANCH LIMITS
    // =================================================
    public int MaxBranches { get; set; }
    public int PurchasedBranches { get; set; }
    public int IncludedBranchesSnapshot { get; set; }
    public int BonusBranches { get; set; }

    // =================================================
    // 👤 USER LIMITS
    // =================================================
    public int MaxUsers { get; set; }
    public int PurchasedUsers { get; set; }
    public int IncludedUsersSnapshot { get; set; }
    public int BonusUsers { get; set; }
    
    // Navigation to latest invoice (optional but helpful for UI cards)
    public Guid? LatestInvoiceId { get; set; }
    public string? LatestInvoiceNumber { get; set; }
    public InvoiceStatus? LatestInvoiceStatus { get; set; }
}