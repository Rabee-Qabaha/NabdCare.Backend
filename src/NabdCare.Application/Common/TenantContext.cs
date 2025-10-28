using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace NabdCare.Application.Common;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public Guid? ClinicId =>
        TryGetGuid("ClinicId");

    public Guid? UserId =>
        TryGetGuid(ClaimTypes.NameIdentifier) ??
        TryGetGuid("sub");

    public string? UserEmail =>
        User?.FindFirst(ClaimTypes.Email)?.Value ??
        User?.FindFirst("email")?.Value;

    public string? UserRole =>
        User?.FindFirst(ClaimTypes.Role)?.Value ??
        User?.FindFirst("role")?.Value;

    public Guid? RoleId =>
        TryGetGuid("RoleId");

    public bool IsSuperAdmin =>
        string.Equals(
            UserRole,
            "SuperAdmin",
            StringComparison.OrdinalIgnoreCase
        );

    private Guid? TryGetGuid(string claimType)
    {
        var value = User?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}