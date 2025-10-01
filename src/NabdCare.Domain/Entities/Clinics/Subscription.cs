using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Clinics;

public class Subscription : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionType Type { get; set; }
    public decimal Fee { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
