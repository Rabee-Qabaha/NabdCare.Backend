using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum AppointmentType
{
    Consultation = 0,  // Standard checkup
    FollowUp = 1,      // Reviewing results
    Procedure = 2,     // Minor surgery/dental work
    Emergency = 3,     // Urgent walk-in
    Telehealth = 4     // Online/Video call
}