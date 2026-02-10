using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class BatchPaymentRequestDto
{
    public Guid? ClinicId { get; set; }
    public Guid? PatientId { get; set; }

    public List<CreatePaymentRequestDto> Payments { get; set; } = new();
    public List<PaymentAllocationRequestDto> InvoicesToPay { get; set; } = new();
}