using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class CreatePaymentRequestDto
{
    public PaymentContext Context { get; set; }
    public Guid? PatientId { get; set; } // Required if Context == Patient
    
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; }
    
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }

    // Optional: Immediately allocate to these invoices
    public List<PaymentAllocationRequestDto> Allocations { get; set; } = new();

    // Optional: Cheque Details
    public CreateChequeDetailDto? ChequeDetail { get; set; }
}

[ExportTsClass]
public class PaymentAllocationRequestDto
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

[ExportTsClass]
public class CreateChequeDetailDto
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string? ImageUrl { get; set; }
}