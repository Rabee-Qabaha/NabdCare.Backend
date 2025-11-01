using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics.Subscriptions;

[ExportTsClass]
public class UpdateSubscriptionRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionType Type { get; set; }
    public decimal Fee { get; set; }
    public SubscriptionStatus Status { get; set; }

    public bool AutoRenew { get; set; } = false;
    public int GracePeriodDays { get; set; } = 0;
}