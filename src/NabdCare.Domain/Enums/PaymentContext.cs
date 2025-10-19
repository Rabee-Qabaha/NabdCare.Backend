using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum PaymentContext
{
    Clinic,
    Patient
}