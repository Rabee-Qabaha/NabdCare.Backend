using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.Users;

public class RolePermission : BaseEntity
{
    [Required]
    public UserRole Role { get; set; }

    [Required]
    public Guid PermissionId { get; set; }
    [ForeignKey(nameof(PermissionId))]
    public AppPermission AppPermission { get; set; }
}