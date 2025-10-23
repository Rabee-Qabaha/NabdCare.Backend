using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Roles;

[ExportTsClass]
public class UpdateRoleRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; } = 100;

    /// <summary>
    /// Hex color code for UI display.
    /// </summary>
    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "ColorCode must be a valid hex color (e.g., #3B82F6)")]
    public string? ColorCode { get; set; }

    /// <summary>
    /// FontAwesome icon class for UI display.
    /// </summary>
    [MaxLength(50)]
    public string? IconClass { get; set; }

    /// <summary>
    /// Whether this role can be used as a template.
    /// Only SuperAdmin can modify this.
    /// </summary>
    public bool IsTemplate { get; set; }
}