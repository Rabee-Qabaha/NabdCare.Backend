using System.ComponentModel.DataAnnotations;

namespace NabdCare.Domain.Entities.Permissions;

public class AppPermission : BaseEntity
{
    [Required]
    [MaxLength(100)] 
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)] 
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}