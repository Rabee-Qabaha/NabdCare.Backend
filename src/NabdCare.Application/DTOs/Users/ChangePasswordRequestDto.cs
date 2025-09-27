using System.ComponentModel.DataAnnotations;

namespace NabdCare.Application.DTOs.Users;


public class ChangePasswordRequestDto
{
    [Required]
    [MinLength(6)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}