using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Roles;

[ExportTsClass]
public class CloneRoleRequestDto
{
    /// <summary>
    /// Target clinic ID for the cloned role.
    /// If null, uses current user's clinic.
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// Name for the cloned role.
    /// If null, uses template name.
    /// </summary>
    [MaxLength(100)]
    public string? NewRoleName { get; set; }

    /// <summary>
    /// Optional custom description.
    /// If null, uses template description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether to copy permissions from the template.
    /// Default: true
    /// </summary>
    public bool CopyPermissions { get; set; } = true;
}