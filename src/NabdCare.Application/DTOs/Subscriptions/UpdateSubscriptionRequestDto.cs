using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Subscriptions;

[ExportTsClass]
public class UpdateSubscriptionRequestDto
{
    // 💰 Paid Add-ons
    public int ExtraBranches { get; set; } 
    public int ExtraUsers { get; set; }

    // 🎁 Deal-Maker Fields
    public int BonusBranches { get; set; } = 0;
    public int BonusUsers { get; set; } = 0;

    // ⚙️ Settings
    public bool AutoRenew { get; set; }
    
    // ✅ Churn Prevention: Allows graceful cancellation via API update
    public bool? CancelAtPeriodEnd { get; set; } 
    
    public int GracePeriodDays { get; set; }
    
    // 👮 Admin Overrides
    public SubscriptionStatus? Status { get; set; }
    public DateTime? EndDate { get; set; }
}