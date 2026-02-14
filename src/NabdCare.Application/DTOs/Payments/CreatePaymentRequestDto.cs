using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class CreatePaymentRequestDto
{
    public PaymentContext Context { get; set; }
    
    public Guid? ClinicId { get; set; } // Required if Context == Clinic (and user is SuperAdmin)
    public Guid? PatientId { get; set; } // Required if Context == Patient
    
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; }
    
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }

    // Optional: Immediately allocate to these invoices
    public List<PaymentAllocationRequestDto> Allocations { get; set; } = new();

    // Optional: Cheque Details
    public CreateChequeDetailDto? ChequeDetail { get; set; }
    
    public decimal? BaseExchangeRate { get; set; }
    public decimal? FinalExchangeRate { get; set; }
}