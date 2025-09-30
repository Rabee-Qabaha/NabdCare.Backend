using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Clinics;

public class CreateClinicRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public ClinicStatus Status { get; set; } = ClinicStatus.Active;
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public decimal SubscriptionFee { get; set; }
}