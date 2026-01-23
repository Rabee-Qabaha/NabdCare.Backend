using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Patients;
using NabdCare.Domain.Entities.Scheduling;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums; // Assume generic enums exist

namespace NabdCare.Domain.Entities.Clinical;

public class ClinicalEncounter : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public DateTime Date { get; set; }

    // --- Standard SOAP Data ---
    public string? ChiefComplaint { get; set; }
    public string? Diagnosis { get; set; }     
    public string? TreatmentPlan { get; set; } 

    // --- Dynamic Specialty Data (JSONB) ---
    /// <summary>
    /// Stores unstructured specialty data (e.g., Tooth Chart, Body Map coordinates).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? SpecialtyData { get; set; }

    /// <summary>
    /// Snapshot of Vitals (BP, Height, Weight) at the time of visit.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? VitalsSnapshot { get; set; }

    public EncounterStatus Status { get; set; } = EncounterStatus.Draft;
    
    // Navigation
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}