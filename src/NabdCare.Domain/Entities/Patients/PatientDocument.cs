using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Patients;

public class PatientDocument : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public DocumentType Type { get; set; } // LabResult, XRay, Referral

    [Required, MaxLength(500)]
    public string StorageUrl { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty; // .pdf, .jpg
    
    public long SizeInBytes { get; set; }
}