using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Users;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Entities.Scheduling;

public class PractitionerSchedule : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    [TsType("string")]
    public TimeSpan StartTime { get; set; }
    [TsType("string")]
    public TimeSpan EndTime { get; set; }

    public bool AllowOnlineBooking { get; set; } = true;

    [MaxLength(50)]
    public string? Label { get; set; }
}