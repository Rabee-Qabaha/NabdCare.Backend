using System.ComponentModel.DataAnnotations;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Auth;

[ExportTsClass]
public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}