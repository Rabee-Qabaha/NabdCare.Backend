using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Auth;

[ExportTsClass]
public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public new string Email { get; set; } = string.Empty;

    [Required]
    public new string Password { get; set; } = string.Empty;
}