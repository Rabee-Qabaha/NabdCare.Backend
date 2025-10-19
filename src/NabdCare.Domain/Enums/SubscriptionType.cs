using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum SubscriptionType
{
    Yearly,
    Monthly,
    Trial
}