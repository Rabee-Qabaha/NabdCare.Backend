
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Auth;

[ExportTsClass]
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public AuthResponseDto(string accessToken)
    {
        AccessToken = accessToken;
    }
}