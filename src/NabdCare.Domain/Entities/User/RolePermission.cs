using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Enums;

namespace NabdCare.Domain.Entities.User;

public class RolePermission : BaseEntity
{
    [Required]
    public UserRole Role { get; set; }

    [Required]
    public Guid PermissionId { get; set; }
    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; }
}