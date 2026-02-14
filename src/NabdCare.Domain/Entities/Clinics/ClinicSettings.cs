using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinics;

public class ClinicSettings
{
    public string TimeZone { get; set; } = "UTC";
    public Currency Currency { get; set; } = Currency.USD;
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string Locale { get; set; } = "en-US";
    
    // Exchange Rate Markup
    public MarkupType ExchangeRateMarkupType { get; set; } = MarkupType.None;
    
    [Column(TypeName = "decimal(5, 2)")]
    public decimal ExchangeRateMarkupValue { get; set; } = 0;

    // Feature flags or other settings can go here
    public bool EnablePatientPortal { get; set; } = true;
}