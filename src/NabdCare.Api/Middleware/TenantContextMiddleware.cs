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
        var clinicIdHeader = context.Request.Headers["X-Clinic-Id"].FirstOrDefault();
        if (Guid.TryParse(clinicIdHeader, out var clinicId))
        {
            tenantContext.ClinicId = clinicId;
        }

        var isSuperAdmin = context.User.IsInRole("SuperAdmin");
        tenantContext.IsSuperAdmin = isSuperAdmin;

        await _next(context);
    }
}