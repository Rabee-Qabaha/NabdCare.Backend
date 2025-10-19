using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Permissions;

[ExportTsClass]
public class UpdatePermissionDto
{
    [Required]
    [MaxLength(30)]
    public string Name { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }
}