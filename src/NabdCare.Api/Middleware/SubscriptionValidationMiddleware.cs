// Middleware/SubscriptionValidationMiddleware.cs
using Microsoft.AspNetCore.Http;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Api.Middleware;

/// <summary>
/// Middleware to block requests from clinics with invalid subscriptions
/// </summary>
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
        // Skip for SuperAdmin
        if (tenantContext.IsSuperAdmin)
        {
            await _next(context);
            return;
        }

        // Skip for auth endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        // Check clinic subscription status
        if (tenantContext.ClinicId.HasValue)
        {
            var clinic = await clinicRepository.GetByIdAsync(tenantContext.ClinicId.Value);
            
            if (clinic == null || 
                clinic.Status == SubscriptionStatus.Expired ||
                clinic.Status == SubscriptionStatus.Suspended ||
                clinic.Status == SubscriptionStatus.Cancelled)
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