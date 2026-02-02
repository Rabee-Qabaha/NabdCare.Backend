using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class CancelPaymentRequestDto
{
    public string Reason { get; set; } = string.Empty;
}