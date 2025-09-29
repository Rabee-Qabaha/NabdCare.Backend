using System.ComponentModel.DataAnnotations;

namespace NabdCare.Application.DTOs.Permissions;

public class CreatePermissionDto
{
    [Required]
    [MaxLength(30)]
    public string Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}