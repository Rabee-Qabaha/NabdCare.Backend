using NabdCare.Application.Common;

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
        // Extract ClinicId from JWT claims or headers
        var clinicIdClaim = context.User.FindFirst("ClinicId")?.Value;
        if (Guid.TryParse(clinicIdClaim, out var clinicId))
        {
            tenantContext.ClinicId = clinicId;
        }
        else
        {
            // Fallback: try header
            var clinicIdHeader = context.Request.Headers["X-Clinic-Id"].FirstOrDefault();
            if (Guid.TryParse(clinicIdHeader, out var headerClinicId))
                tenantContext.ClinicId = headerClinicId;
        }

        // IsSuperAdmin (from role)
        tenantContext.IsSuperAdmin = context.User.IsInRole("SuperAdmin");

        await _next(context);
    }
}