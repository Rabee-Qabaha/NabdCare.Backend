using System.ComponentModel.DataAnnotations;

namespace NabdCare.Domain.DTOs.Auth;

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}