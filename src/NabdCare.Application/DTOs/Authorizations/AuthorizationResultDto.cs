using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Authorizations;

/// <summary>
/// Response DTO for authorization check results.
/// Auto-exported to frontend via TypeGen.
/// </summary>
[ExportTsClass]
public class AuthorizationResultDto
{
    /// <summary>
    /// Whether the user is authorized to perform the action on the resource
    /// </summary>
    public bool Allowed { get; set; }

    /// <summary>
    /// Reason why access was denied (null if allowed)
    /// Examples: "User belongs to a different clinic", "Resource not found", "Permission required"
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Policy that evaluated this authorization (e.g., "UserPolicy", "ClinicPolicy")
    /// Useful for debugging and audit logging
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// Echo back the resource type for correlation
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Echo back the action for correlation
    /// </summary>
    public string Action { get; set; } = string.Empty;
}