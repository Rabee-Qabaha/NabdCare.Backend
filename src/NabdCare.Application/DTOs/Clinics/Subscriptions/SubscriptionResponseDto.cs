using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Clinics.Subscriptions;
public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionType Type { get; set; }
    public decimal Fee { get; set; }
    public SubscriptionStatus Status { get; set; }
}