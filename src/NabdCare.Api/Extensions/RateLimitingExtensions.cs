using Microsoft.AspNetCore.RateLimiting;
using NabdCare.Api.Configurations;

namespace NabdCare.Api.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting configuration for the application.
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Bind rate limiting settings from appsettings.json
        var rateLimitSettings = configuration
            .GetSection("RateLimiting")
            .Get<RateLimitSettings>() ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            // Auth endpoints: configurable from appsettings.json
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(rateLimitSettings.Auth.WindowMinutes);
                opt.PermitLimit = rateLimitSettings.Auth.PermitLimit;
                opt.QueueLimit = 0;
            });
            
            // Custom rejection handler with consistent error format
            options.OnRejected = async (context, cancellationToken) =>
            {
                var traceId = Guid.NewGuid().ToString("N");
                context.HttpContext.Response.Headers["X-Trace-Id"] = traceId;
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "Too many requests. Please try again later.",
                        type = "RateLimitExceeded",
                        statusCode = 429,
                        traceId,
                        retryAfterSeconds = rateLimitSettings.Auth.WindowMinutes * 60
                    }
                }, cancellationToken);
            };
        });

        return services;
    }
}