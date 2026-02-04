using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class PaymentAllocationRequestDto
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
}