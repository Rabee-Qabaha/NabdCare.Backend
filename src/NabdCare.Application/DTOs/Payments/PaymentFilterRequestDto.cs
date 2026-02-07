using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class PaymentFilterRequestDto : PaginationRequestDto
{
    public PaymentMethod? Method { get; set; }
    public PaymentStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Reference { get; set; } // TransactionId or ChequeNumber
}