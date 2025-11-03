using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Authorizations;

/// <summary>
/// Request DTO for checking authorization on a specific resource.
/// Auto-exported to frontend via TypeGen.
/// </summary>
[ExportTsClass]
public class AuthorizationCheckRequestDto
{
    /// <summary>
    /// Type of resource: "user", "clinic", "role", "subscription", "patient", "payment", "medicalrecord", "appointment"
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource ID (GUID) as string
    /// </summary>
    [Required]
    [MinLength(36)]
    [MaxLength(36)]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Action to check: "view", "edit", "delete", "create"
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;
}