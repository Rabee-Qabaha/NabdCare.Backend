using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ClinicSettingsDto
{
    public string TimeZone { get; set; } = "UTC";
    public string Currency { get; set; } = "USD";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string Locale { get; set; } = "en-US";
    public bool EnablePatientPortal { get; set; } = false;
}