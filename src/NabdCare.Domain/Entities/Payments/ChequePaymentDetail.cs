using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Payments;

public class ChequePaymentDetail : BaseEntity
{
    [Required]
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    [Required, MaxLength(50)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Branch { get; set; } = string.Empty;

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Cheque amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public ChequeStatus Status { get; set; } = ChequeStatus.Pending;

    public DateTime? ClearedDate { get; set; }

    public string? ImageUrl { get; set; } = string.Empty;
}