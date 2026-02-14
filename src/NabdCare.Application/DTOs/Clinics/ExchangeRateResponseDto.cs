using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ExchangeRateResponseDto
{
    public decimal BaseRate { get; set; }
    public decimal FinalRate { get; set; }
    public MarkupType MarkupType { get; set; }
    public decimal MarkupValue { get; set; }
    public string FunctionalCurrency { get; set; }
    public string TargetCurrency { get; set; }
}