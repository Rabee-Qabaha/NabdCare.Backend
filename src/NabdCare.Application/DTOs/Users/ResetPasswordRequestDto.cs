using System.ComponentModel.DataAnnotations;

namespace NabdCare.Application.DTOs.Users;

public class ResetPasswordRequestDto
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}