using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ClinicSettingsDto
{
    public string TimeZone { get; set; } = "UTC";
    public Currency Currency { get; set; } = Currency.USD;
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string Locale { get; set; } = "en-US";
    
    // Exchange Rate Markup
    public MarkupType ExchangeRateMarkupType { get; set; } = MarkupType.None;
    public decimal ExchangeRateMarkupValue { get; set; } = 0;

    public bool EnablePatientPortal { get; set; } = false;
}