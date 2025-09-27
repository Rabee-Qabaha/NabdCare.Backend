using System.ComponentModel.DataAnnotations;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.DTOs.Users;

public class CreateUserRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    // Only SuperAdmin can override, ClinicAdmin defaults to their clinic
    public Guid? ClinicId { get; set; }
}
