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

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ILogger<TenantContextMiddleware> logger)
    {
        if (context.User?.Identity is { IsAuthenticated: true })
        {
            var clinicClaim = context.User.FindFirst("ClinicId")?.Value;
            if (Guid.TryParse(clinicClaim, out var clinicId))
            {
                tenantContext.ClinicId = clinicId;
            }
            else
            {
                logger.LogWarning("Missing or invalid ClinicId claim for user {User}", context.User.Identity?.Name);
            }

            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                            ?? context.User.FindFirst("role")?.Value;

            if (string.Equals(roleClaim, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                tenantContext.IsSuperAdmin = true;
            }
        }

        await _next(context);
    }
}