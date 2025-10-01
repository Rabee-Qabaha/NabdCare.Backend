using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinics;

public class Clinic : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    // Each clinic can have one active subscription
    public ICollection<Subscription>? Subscriptions { get; set; } = new List<Subscription>();

    public int BranchCount { get; set; } = 1;
}