using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Roles;

// [ExportTsClass]
[ExportTsInterface]
public class CreateRoleRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// If null, creates role for current user's clinic (inferred from context).
    /// SuperAdmin can specify any clinicId or null for system-level roles.
    /// </summary>
    public Guid? ClinicId { get; set; }

    /// <summary>
    /// If true, this role can be used as a template for other clinics.
    /// Only SuperAdmin can create template roles.
    /// </summary>
    public bool IsTemplate { get; set; } = false;

    /// <summary>
    /// Display order for UI sorting (lower numbers appear first).
    /// Default: 100
    /// </summary>
    public int DisplayOrder { get; set; } = 100;

    /// <summary>
    /// Hex color code for UI display (e.g., "#3B82F6").
    /// Optional.
    /// </summary>
    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "ColorCode must be a valid hex color (e.g., #3B82F6)")]
    public string? ColorCode { get; set; }

    /// <summary>
    /// FontAwesome icon class for UI display (e.g., "fa-user-md").
    /// Optional.
    /// </summary>
    [MaxLength(50)]
    public string? IconClass { get; set; }

    /// <summary>
    /// Optional: If creating from a template, specify the template role ID.
    /// Permissions will be copied from the template.
    /// </summary>
    public Guid? TemplateRoleId { get; set; }
}