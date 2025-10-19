using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Permissions;

[ExportTsClass]
public class AssignPermissionToUserDto
{
    [Required]
    public Guid PermissionId { get; set; }

    [Required]
    public Guid UserId { get; set; }
}