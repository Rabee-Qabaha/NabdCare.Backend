using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Invoices;

[ExportTsClass]
public class WriteOffRequestDto
{
    public string Reason { get; set; } = string.Empty;
}