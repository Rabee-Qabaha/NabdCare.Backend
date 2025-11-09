using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Domain.Entities.Roles;

public class RolePermission : BaseEntity
{
    [Required]
    public Guid RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [Required]
    public Guid PermissionId { get; set; }
    [ForeignKey(nameof(PermissionId))]
    public AppPermission AppPermission { get; set; } = null!;
}