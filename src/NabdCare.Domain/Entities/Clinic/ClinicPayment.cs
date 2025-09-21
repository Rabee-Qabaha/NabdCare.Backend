using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinic;

public class ClinicPayment : BaseEntity
{
    [Required]
    public Guid ClinicId { get; set; }

    [ForeignKey(nameof(ClinicId))]
    public Clinic Clinic { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    public string? Notes { get; set; }
}