using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.AuditLogs;

/// <summary>
/// Filters used to query audit logs.
/// Pagination is handled separately via PaginationRequestDto.
/// </summary>
[ExportTsClass]
public class AuditLogListRequestDto
{
    /// <summary>
    /// Filter by action type (e.g., "Create", "Update", "Delete").
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by entity type (e.g., "User", "Appointment").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by specific user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Start date range filter (UTC).
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// End date range filter (UTC).
    /// </summary>
    public DateTime? End { get; set; }

    /// <summary>
    /// Free-text search (reason, changes, user email, etc.).
    /// </summary>
    public string? Search { get; set; }
    
    /// <summary>
    /// Filter by clinic ID (multi-tenant isolation)
    /// </summary>
    public Guid? ClinicId { get; set; }
}