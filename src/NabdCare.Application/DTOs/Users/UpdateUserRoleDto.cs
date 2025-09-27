using NabdCare.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NabdCare.Application.DTOs.Users;

public class UpdateUserRoleDto
{
    [Required]
    public UserRole Role { get; set; }
}