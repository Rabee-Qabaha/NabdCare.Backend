using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Permissions;

[ExportTsClass]
public class AssignPermissionDto
{
    [Required]
    public Guid PermissionId { get; set; }

    [Required]
    public UserRole Role { get; set; }
}