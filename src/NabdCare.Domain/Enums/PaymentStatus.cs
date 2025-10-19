using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}
