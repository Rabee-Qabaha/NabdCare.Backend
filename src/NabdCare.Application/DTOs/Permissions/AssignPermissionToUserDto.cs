using System.ComponentModel.DataAnnotations;

namespace NabdCare.Application.DTOs.Permissions;

public class AssignPermissionToUserDto
{
    [Required]
    public Guid PermissionId { get; set; }

    [Required]
    public Guid UserId { get; set; }
}