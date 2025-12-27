namespace NabdCare.Domain.Entities.Clinics;

public class ClinicSettings
{
    public string TimeZone { get; set; } = "UTC";
    public string Currency { get; set; } = "USD";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string Locale { get; set; } = "en-US";
    
    // Feature flags or other settings can go here
    public bool EnablePatientPortal { get; set; } = true;
}