using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class CreateClinicRequestDto
{
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique URL-safe identifier for the clinic (e.g., 'ramallah-clinic').
    /// Used for subdomains: ramallah-clinic.nabdcare.com
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // ==========================================
    // 🎨 Branding & Legal
    // ==========================================
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    
    // ==========================================
    // ⚙️ Configuration
    // ==========================================
    public ClinicSettingsDto? Settings { get; set; }

    // ==========================================
    // 💳 Subscription (Initial)
    // ==========================================
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public decimal SubscriptionFee { get; set; }
    public int BranchCount { get; set; } = 1;
}