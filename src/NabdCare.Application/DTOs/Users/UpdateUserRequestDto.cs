using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Users;

public class UpdateUserRequestDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public UserRole Role { get; set; }
}