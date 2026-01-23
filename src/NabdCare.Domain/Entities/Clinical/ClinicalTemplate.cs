using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Domain.Entities.Clinical;

/// <summary>
/// Defines the dynamic form structure for different specialties.
/// </summary>
public class ClinicalTemplate : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Dental Initial Exam"

    [Required, MaxLength(50)]
    public string Specialty { get; set; } = "General"; // Dental, Derma, Physio

    /// <summary>
    /// JSON Schema defining the UI fields.
    /// Example: [ { "key": "tooth_18", "type": "tooth_selector", "label": "Upper Right Wisdom" } ]
    /// </summary>
    [Column(TypeName = "jsonb")] 
    public string FormSchema { get; set; } = "[]";

    public bool IsActive { get; set; } = true;
}