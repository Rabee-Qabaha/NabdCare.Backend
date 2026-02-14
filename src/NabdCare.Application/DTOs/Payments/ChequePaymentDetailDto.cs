using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class ChequePaymentDetailDto
{
    public string ChequeNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public ChequeStatus Status { get; set; }
    public DateTime? ClearedDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? Note { get; set; }
}