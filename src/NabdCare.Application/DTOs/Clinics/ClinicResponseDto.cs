using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ClinicResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    // Branding
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? TaxNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    
    public ClinicSettingsDto Settings { get; set; } = new();

    // ==========================================
    // ⚠️ CRITICAL UPDATE: Nullable Subscription Data
    // ==========================================
    public SubscriptionStatus Status { get; set; } // Enum defaults to 0, which is usually 'Pending' or 'Inactive'
    
    // These must be nullable to correctly represent "No Subscription yet"
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public SubscriptionType? SubscriptionType { get; set; }
    public decimal? SubscriptionFee { get; set; }
    
    public int BranchCount { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}