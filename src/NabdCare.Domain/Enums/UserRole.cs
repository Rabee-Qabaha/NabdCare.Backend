using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum UserRole
{
    SuperAdmin,
    ClinicAdmin,
    Doctor,
    Nurse,
    Receptionist
}