using System.Security.Claims;
using NabdCare.Application.Common;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Middleware;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Ensure the user is authenticated
        if (context.User?.Identity != null && context.User.Identity.IsAuthenticated)
        {
            // Extract ClinicId from claim (you must include it when issuing JWT)
            var clinicClaim = context.User.FindFirst("ClinicId")?.Value;
            if (Guid.TryParse(clinicClaim, out var clinicId))
            {
                tenantContext.ClinicId = clinicId;
            }

            // Extract role or a claim indicating superadmin
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                            ?? context.User.FindFirst("role")?.Value;

            if (!string.IsNullOrEmpty(roleClaim) && roleClaim == UserRole.SuperAdmin.ToString())
            {
                tenantContext.IsSuperAdmin = true;
            }
        }

        // If not authenticated, leave tenantContext defaults (ClinicId null, IsSuperAdmin false)

        // Call the next middleware
        await _next(context);
    }
}