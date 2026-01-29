using NabdCare.Application.DTOs.Clinics;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class CreateClinicRequestDto
{
    // ==========================================
    // 🆔 Identity
    // ==========================================
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique URL-safe identifier (e.g., 'ramallah-clinic').
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Address { get; set; } = string.Empty;
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
}