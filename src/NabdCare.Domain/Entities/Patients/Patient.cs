using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Clinical;
using NabdCare.Domain.Entities.Scheduling;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Patients;

public class Patient : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    [Required, MaxLength(50)]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public BloodType? BloodType { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    [MaxLength(100)]
    public string? Email { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }

    // Critical Medical Flags
    public bool HasAllergies { get; set; }
    public bool HasChronicConditions { get; set; }
    [MaxLength(1000)]
    public string? MedicalAlertNote { get; set; }

    // Navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<ClinicalEncounter> Encounters { get; set; } = new List<ClinicalEncounter>();
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}