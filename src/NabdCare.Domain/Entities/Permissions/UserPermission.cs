using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Domain.Entities.Permissions;

public class UserPermission : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    [Required]
    public Guid PermissionId { get; set; }
    [ForeignKey(nameof(PermissionId))]
    public AppPermission AppPermission { get; set; }
}