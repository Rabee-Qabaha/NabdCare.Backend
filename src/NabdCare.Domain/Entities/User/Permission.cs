using System.ComponentModel.DataAnnotations;

namespace NabdCare.Domain.Entities.User;

public class Permission : BaseEntity
{
    [Required]
    [MaxLength(30)]
    public string Name { get; set; } // e.g. "ViewPatients", "EditClinic", etc.

    [MaxLength(255)]
    public string? Description { get; set; }
}