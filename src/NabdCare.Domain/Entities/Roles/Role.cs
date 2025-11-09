using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Domain.Entities.Roles;

/// <summary>
/// Dynamic role entity supporting both SaaS-level and tenant-level roles
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// Role name (e.g., "SuperAdmin", "Senior Doctor", "Lab Technician")
    /// Unique per clinic (not globally unique)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the role's purpose
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Multi-tenancy support:
    /// - null = System-wide role (SaaS level) or Template role
    /// - Guid = Clinic-specific role
    /// </summary>
    public Guid? ClinicId { get; set; }
    [ForeignKey(nameof(ClinicId))]
    public Clinic? Clinic { get; set; }

    /// <summary>
    /// System roles are created by SuperAdmin and cannot be deleted
    /// Examples: SuperAdmin, SupportManager, BillingManager
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Template roles are read-only blueprints that clinics can clone
    /// Examples: Doctor, Nurse, Receptionist, Clinic Admin
    /// </summary>
    public bool IsTemplate { get; set; } = false;

    /// <summary>
    /// If this role was cloned from a template, this references the original
    /// Used for tracking template relationships
    /// </summary>
    public Guid? TemplateRoleId { get; set; }
    [ForeignKey(nameof(TemplateRoleId))]
    public Role? TemplateRole { get; set; }

    /// <summary>
    /// Display order for sorting in UI (lower numbers appear first)
    /// System roles: 1-9, Templates: 10-19, Custom: 20+
    /// </summary>
    public int DisplayOrder { get; set; } = 100;

    /// <summary>
    /// Hex color code for UI display (e.g., "#DC2626" for red)
    /// </summary>
    [MaxLength(7)]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Optional icon class for UI display
    /// Examples: "fa-user-md", "lucide-stethoscope", "mdi-doctor"
    /// </summary>
    [MaxLength(50)]
    public string? IconClass { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}