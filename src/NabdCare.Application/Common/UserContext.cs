using System.Security.Claims;
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
        return _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    }

    public string? GetCurrentUserRoleId()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindFirst("RoleId")?.Value;
    }
}