using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class ResetPasswordRequestDto
{
    public string NewPassword { get; set; } = string.Empty;
}