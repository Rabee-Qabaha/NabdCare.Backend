using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Patients;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Payments;

public class Payment : BaseEntity
{
    [Required]
    public PaymentContext Context { get; set; }

    // Foreign Keys (only one must be filled depending on Context)
    public Guid? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    public PaymentMethod Method { get; set; }

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    // Navigation for cheque
    public ChequePaymentDetail? ChequeDetail { get; set; }
}