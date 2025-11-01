using System.Security.Claims;

namespace NabdCare.Api.Extensions;

public static class ClaimsExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var parsed) ? parsed : null;
    }

    public static Guid? GetRoleId(this ClaimsPrincipal user)
    {
        var id = user.FindFirst("roleId")?.Value;
        return Guid.TryParse(id, out var parsed) ? parsed : null;
    }

    public static Guid? GetClinicId(this ClaimsPrincipal user)
    {
        var id = user.FindFirst("ClinicId")?.Value;
        return Guid.TryParse(id, out var parsed) ? parsed : null;
    }
}