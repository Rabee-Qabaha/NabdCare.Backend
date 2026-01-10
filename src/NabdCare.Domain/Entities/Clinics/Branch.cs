namespace NabdCare.Domain.Entities.Clinics;

public class Branch : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; } 

    public bool IsActive { get; set; } = true; 
    public bool IsMain { get; set; } = false; 
}