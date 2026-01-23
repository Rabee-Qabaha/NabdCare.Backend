using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Patients;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Domain.Entities.Clinical;

public class Prescription : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;

    public Guid? ClinicalEncounterId { get; set; }
    public ClinicalEncounter? ClinicalEncounter { get; set; }

    [Required, MaxLength(200)]
    public string MedicationName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Frequency { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Duration { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Instructions { get; set; }
}