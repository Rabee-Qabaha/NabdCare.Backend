using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum AppointmentStatus
{
    Scheduled = 0,   // Booked but not yet arrived
    Confirmed = 1,   // Patient confirmed via SMS/Call
    CheckedIn = 2,   // Patient is in the waiting room
    InProgress = 3,  // Patient is with the doctor (Encounter Created)
    Completed = 4,   // Visit done, patient left
    Canceled = 5,    // Canceled in advance
    NoShow = 6       // Patient did not come, no warning
}