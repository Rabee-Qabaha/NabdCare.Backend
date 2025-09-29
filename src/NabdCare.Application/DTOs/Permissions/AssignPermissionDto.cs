using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Permissions;

public class AssignPermissionDto
{
    [Required]
    public Guid PermissionId { get; set; }

    [Required]
    public UserRole Role { get; set; }
}