using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NabdCare.Domain.Entities.Permissions;

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