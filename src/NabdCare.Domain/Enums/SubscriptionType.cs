using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum SubscriptionType
{
    Monthly = 0,
    Yearly = 1,
    Lifetime = 2
}