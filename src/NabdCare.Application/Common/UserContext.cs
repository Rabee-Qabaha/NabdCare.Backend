using Microsoft.AspNetCore.Http;
using NabdCare.Application.Common;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserId()
    {
        // ✅ "sub" claim (from JWT) holds the user ID
        return _httpContextAccessor.HttpContext?.User?
                   .FindFirst("sub")?.Value 
               ?? "anonymous";
    }

    public string? GetCurrentUserRoleId()
    {
        var claims = _httpContextAccessor.HttpContext?.User?.Claims;
        return claims?.FirstOrDefault(c => 
            string.Equals(c.Type, "roleId", StringComparison.OrdinalIgnoreCase)
        )?.Value;
    }

    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst("email")?.Value;
    }
}