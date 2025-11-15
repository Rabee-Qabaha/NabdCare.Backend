using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Roles;

// [ExportTsClass]
[ExportTsInterface]
public class RoleResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// If true, this is a system role (SuperAdmin, SupportManager, etc.).
    /// System roles cannot be modified or deleted.
    /// </summary>
    public bool IsSystemRole { get; set; }
    
    /// <summary>
    /// If true, this role can be cloned by clinics.
    /// </summary>
    public bool IsTemplate { get; set; }
    
    /// <summary>
    /// Clinic this role belongs to. Null for system roles and templates.
    /// </summary>
    public Guid? ClinicId { get; set; }
    
    /// <summary>
    /// Clinic name (if ClinicId is set).
    /// </summary>
    public string? ClinicName { get; set; }
    
    /// <summary>
    /// If this role was cloned from a template, this is the template's ID.
    /// </summary>
    public Guid? TemplateRoleId { get; set; }
    
    /// <summary>
    /// Display order for UI sorting.
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// Hex color code for UI display.
    /// </summary>
    public string? ColorCode { get; set; }
    
    /// <summary>
    /// FontAwesome icon class.
    /// </summary>
    public string? IconClass { get; set; }
    
    /// <summary>
    /// Number of users currently assigned to this role.
    /// </summary>
    public int UserCount { get; set; }
    
    /// <summary>
    /// Number of permissions assigned to this role.
    /// </summary>
    public int PermissionCount { get; set; }

    // ✅ Audit fields from BaseEntity
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ✅ Display names for audit trail (NEW)
    public string? CreatedByUserName { get; set; }
    public string? UpdatedByUserName { get; set; }
    public string? DeletedByUserName { get; set; }
}