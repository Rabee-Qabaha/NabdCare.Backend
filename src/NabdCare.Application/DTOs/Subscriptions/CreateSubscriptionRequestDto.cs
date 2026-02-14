using TypeGen.Core.TypeAnnotations;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Subscriptions;

[ExportTsClass]
public class CreateSubscriptionRequestDto
{
    public Guid ClinicId { get; set; }
    public string PlanId { get; set; } = string.Empty;

    // Multi-Currency Support (e.g. "USD", "EUR")
    // Defaults to system setting if null
    // [REMOVED] Public Currency? Currency { get; set; } - Now handled by SaaSSettings 

    // 💰 Paid Add-ons (Multiplied by Price)
    public int ExtraBranches { get; set; } = 0;
    public int ExtraUsers { get; set; } = 0;

    // 🎁 Deal-Maker Fields (Free)
    // These get added to the 'Included' snapshot, effectively increasing the base plan limit for free.
    public int BonusBranches { get; set; } = 0;
    public int BonusUsers { get; set; } = 0;

    public bool AutoRenew { get; set; } = true;
    public DateTime? CustomStartDate { get; set; }
}