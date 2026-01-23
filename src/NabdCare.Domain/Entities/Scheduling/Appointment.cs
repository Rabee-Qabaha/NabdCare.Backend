using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Entities.Patients;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Scheduling;

public class Appointment : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;

    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public AppointmentType Type { get; set; }

    [MaxLength(500)]
    public string? ReasonForVisit { get; set; }
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Links
    public Guid? ClinicalEncounterId { get; set; } 
}