using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinic;

public class Clinic : BaseEntity
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(15)]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    [Required]
    public ClinicStatus Status { get; set; } = ClinicStatus.Active;

    [Required]
    public DateTime SubscriptionStartDate { get; set; }

    [Required]
    public DateTime SubscriptionEndDate { get; set; }

    [Required]
    public SubscriptionType SubscriptionType { get; set; }

    [Required]
    public decimal SubscriptionFee { get; set; }

    [Required]
    public Guid CreatedByAdminId { get; set; }
}