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
        // 1. Skip checks for Public endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth") || 
            context.Request.Path.StartsWithSegments("/api/webhooks"))
        {
            await _next(context);
            return;
        }

        // 2. Skip for SuperAdmin
        if (tenantContext.IsSuperAdmin)
        {
            await _next(context);
            return;
        }

        // 3. CHECK SUBSCRIPTION
        if (tenantContext.ClinicId.HasValue)
        {
            // Optimization: Cache this in Redis in production!
            var clinic = await clinicRepository.GetByIdAsync(tenantContext.ClinicId.Value);

            if (clinic == null || IsAccessRestricted(clinic.Status))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "ACCESS_DENIED",
                    Code = GetErrorCode(clinic?.Status),
                    Message = GetStatusMessage(clinic?.Status)
                });
                return; 
            }
        }

        await _next(context);
    }

    private static bool IsAccessRestricted(SubscriptionStatus status)
    {
        // ✅ Allow ACTIVE and TRIAL
        if (status == SubscriptionStatus.Active || status == SubscriptionStatus.Trial)
            return false;

        // 🛑 Block EVERYTHING else (Expired, Suspended, Cancelled, Future)
        return true;
    }

    private static string GetErrorCode(SubscriptionStatus? status)
    {
        return status switch
        {
            SubscriptionStatus.Expired => "SUBSCRIPTION_EXPIRED",
            SubscriptionStatus.Suspended => "ACCOUNT_SUSPENDED",
            SubscriptionStatus.Cancelled => "SUBSCRIPTION_CANCELLED",
            SubscriptionStatus.Future => "SUBSCRIPTION_FUTURE_NOT_STARTED",
            _ => "SUBSCRIPTION_INACTIVE"
        };
    }

    private static string GetStatusMessage(SubscriptionStatus? status)
    {
        return status switch
        {
            SubscriptionStatus.Expired => "Your subscription has expired. Please renew to continue.",
            SubscriptionStatus.Suspended => "Your account is suspended. Please contact support.",
            SubscriptionStatus.Cancelled => "Your subscription has been cancelled.",
            SubscriptionStatus.Future => "Your subscription start date has not arrived yet.",
            _ => "Service unavailable. No active subscription found."
        };
    }
}