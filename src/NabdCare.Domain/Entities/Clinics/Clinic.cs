using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinics;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    // Example: "ramallah-medic" -> ramallah-medic.nabdcare.com
    public string Slug { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // ==========================================
    // 🎨 Branding & Legal (New)
    // ==========================================
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }        
    public string? RegistrationNumber { get; set; }

    // ==========================================
    // ⚙️ Configuration (New - Mapped to JSONB)
    // ==========================================
    public ClinicSettings Settings { get; set; } = new();

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public ICollection<Subscription>? Subscriptions { get; set; } = new List<Subscription>();

    public int BranchCount { get; set; } = 1;
}