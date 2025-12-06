using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Roles;

// [ExportTsClass]
[ExportTsInterface]
public class CloneRoleRequestDto
{
    /// <summary>
    /// Target clinic ID for the cloned role.
    /// If null, uses current user's clinic.
    /// </summary>
    [TsNull]
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// Name for the cloned role.
    /// If null, uses template name (e.g. "Doctor (Copy)").
    /// </summary>
    [MaxLength(100)]
    [TsNull]
    public string? NewRoleName { get; set; }

    /// <summary>
    /// Optional custom description.
    /// If null, uses template description.
    /// </summary>
    [MaxLength(500)]
    [TsNull]
    public string? Description { get; set; }

    /// <summary>
    /// Hex color code (e.g., #3B82F6).
    /// If null, copies the template's color.
    /// </summary>
    [MaxLength(7)]
    [TsNull]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Icon class (e.g., "pi pi-user").
    /// If null, copies the template's icon.
    /// </summary>
    [MaxLength(50)]
    [TsNull]
    public string? IconClass { get; set; }

    /// <summary>
    /// Display order for sorting.
    /// If null, copies template order (or appends to end if logic exists).
    /// </summary>
    [TsNull]
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Whether to copy permissions from the template.
    /// Default: true
    /// </summary>
    [TsNull]
    public bool CopyPermissions { get; set; } = true;
}