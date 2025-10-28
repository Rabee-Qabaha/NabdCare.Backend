using Microsoft.AspNetCore.Http;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Middleware;

public class SubscriptionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SubscriptionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IClinicRepository clinicRepository)
    {
        // ✅ Allow requests authenticated via JWT Bearer header (Swagger usage)
        var hasJwtHeader = context.Request.Headers["Authorization"]
            .Any(h => h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));

        if (hasJwtHeader)
        {
            await _next(context);
            return;
        }

        // ✅ Skip for SuperAdmin (always allowed)
        if (tenantContext.IsSuperAdmin)
        {
            await _next(context);
            return;
        }

        // ✅ Skip checks for login/refresh/etc.
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        if (tenantContext.ClinicId.HasValue)
        {
            var clinic = await clinicRepository.GetByIdAsync(tenantContext.ClinicId.Value);

            if (clinic == null ||
                clinic.Status is SubscriptionStatus.Expired or
                SubscriptionStatus.Suspended or
                SubscriptionStatus.Cancelled)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Clinic subscription is not active",
                    Status = clinic?.Status.ToString() ?? "Unknown",
                    Message = GetStatusMessage(clinic?.Status)
                });
                return;
            }
        }

        await _next(context);
    }

    private static string GetStatusMessage(SubscriptionStatus? status)
    {
        return status switch
        {
            SubscriptionStatus.Expired => "Your subscription has expired. Please contact support to renew.",
            SubscriptionStatus.Suspended => "Your clinic has been suspended. Please contact support.",
            SubscriptionStatus.Cancelled => "Your subscription has been cancelled.",
            _ => "Your clinic is not active."
        };
    }
}