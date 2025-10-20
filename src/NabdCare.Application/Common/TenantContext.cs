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

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? ClinicId
    {
        get
        {
            var clinicIdClaim = User?.FindFirst("ClinicId")?.Value;
            return Guid.TryParse(clinicIdClaim, out var clinicId) ? clinicId : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            // Try standard claim first, then custom "sub" claim
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User?.FindFirst("sub")?.Value;
            
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? UserEmail =>
        User?.FindFirst(ClaimTypes.Email)?.Value 
        ?? User?.FindFirst("email")?.Value;

    public bool IsSuperAdmin
    {
        get
        {
            var role = User?.FindFirst(ClaimTypes.Role)?.Value 
                       ?? User?.FindFirst("role")?.Value;

            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string? UserRole =>
        User?.FindFirst(ClaimTypes.Role)?.Value 
        ?? User?.FindFirst("role")?.Value;
}