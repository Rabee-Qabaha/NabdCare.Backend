using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class RefundRequestDto
{
    public string Reason { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}