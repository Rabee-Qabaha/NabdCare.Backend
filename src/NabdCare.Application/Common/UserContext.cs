using Microsoft.AspNetCore.Http;

namespace NabdCare.Application.Common;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserId()
    {
        // ✅ Prefer "sub" claim because DefaultMapInboundClaims=false
        return _httpContextAccessor.HttpContext?.User?
                   .FindFirst("sub")?.Value 
               ?? "anonymous";
    }

    public string? GetCurrentUserRoleId()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst("RoleId")?.Value;
    }

    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst("email")?.Value;
    }
}