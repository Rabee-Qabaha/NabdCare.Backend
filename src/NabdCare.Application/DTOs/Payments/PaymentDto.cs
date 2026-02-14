using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class PaymentDto
{
    public Guid Id { get; set; }
    public PaymentContext Context { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid? PatientId { get; set; }
    
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal BaseExchangeRate { get; set; }
    public decimal FinalExchangeRate { get; set; }
    public decimal AmountInFunctionalCurrency { get; set; }
    public decimal UnallocatedAmount { get; set; }
    
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    
    public string? TransactionId { get; set; }
    public string? Notes { get; set; }
    
    public ChequePaymentDetailDto? ChequeDetail { get; set; }
    public List<PaymentAllocationDto> Allocations { get; set; } = new();
}