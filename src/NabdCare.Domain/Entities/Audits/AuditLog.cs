using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NabdCare.Domain.Entities.Audits;

/// <summary>
/// GDPR-compliant audit log for tracking sensitive operations
/// This table is INSERT-ONLY (no updates or deletes allowed)
/// </summary>
public class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    #region Who (Actor Information)

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Email of the user (for readability, since users can be deleted)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Tenant context (null for SaaS-level actions)
    /// </summary>
    public Guid? ClinicId { get; set; }

    #endregion

    #region What (Entity & Action)

    /// <summary>
    /// Type of entity affected (e.g., "Role", "User", "Permission", "Patient")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity (null for system-level actions)
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Action performed (e.g., "Created", "Updated", "Deleted", "Accessed", "PermissionAdded")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    #endregion

    #region Details

    /// <summary>
    /// JSON representation of changes made
    /// Format: {"FieldName": {"Old": "value1", "New": "value2"}}
    /// Example: {"Name": {"Old": "Doctor", "New": "Senior Doctor"}}
    /// </summary>
    [Column(TypeName = "jsonb")] // PostgreSQL JSONB for efficient querying
    public string? Changes { get; set; }

    /// <summary>
    /// Optional reason or notes for the action (user-provided)
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    #endregion

    #region When & Where

    /// <summary>
    /// When the action occurred (UTC)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the request
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent (browser/client information)
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    #endregion
}