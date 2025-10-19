using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum SubscriptionStatus
{
    Active,
    Expired,
    Cancelled
}