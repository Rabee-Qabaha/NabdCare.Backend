using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Clinics.Subscriptions;
public class UpdateSubscriptionRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionType Type { get; set; }
    public decimal Fee { get; set; }
    public SubscriptionStatus Status { get; set; }
}