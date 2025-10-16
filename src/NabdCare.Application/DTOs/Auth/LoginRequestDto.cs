using System.ComponentModel.DataAnnotations;

namespace NabdCare.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public new string Email { get; set; } = string.Empty;

    [Required]
    public new string Password { get; set; } = string.Empty;
}