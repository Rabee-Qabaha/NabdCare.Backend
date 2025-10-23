using System.Security.Claims;
using System.Threading.RateLimiting;
using NabdCare.Api.Configurations;

namespace NabdCare.Api.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting configuration for the application.
    /// - "auth" policy: fixed window partitioned by client IP (use for login)
    /// - "api" policy: token-bucket partitioned by authenticated user id (use for normal API endpoints)
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
            // Use a partitioned policy approach so one client cannot exhaust the global quota
            // AUTH: fixed-window per client IP (good for login endpoints)
            options.AddPolicy("auth", httpContext =>
            {
                var authSettings = rateLimitSettings.Auth;
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, authSettings.PermitLimit),
                        Window = TimeSpan.FromMinutes(Math.Max(1, authSettings.WindowMinutes)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = authSettings.QueueLimit
                    });
            });

            // API: token-bucket per authenticated user id, fallback to IP for anonymous
            options.AddPolicy("api", httpContext =>
            {
                var apiSettings = rateLimitSettings.Api;

                // Partition by user id claim if present, otherwise by client IP
                var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var partitionKey = !string.IsNullOrWhiteSpace(userId)
                    ? $"user:{userId}"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey,
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = Math.Max(1, apiSettings.PermitLimit),
                        TokensPerPeriod = Math.Max(1, apiSettings.PermitLimit),
                        ReplenishmentPeriod = TimeSpan.FromMinutes(Math.Max(1, apiSettings.WindowMinutes)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = apiSettings.QueueLimit
                    });
            });

            // Custom rejection handler with consistent error format
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                // If configuration exists, use its window to compute retry info
                var retryAfterSeconds = (rateLimitSettings.Auth?.WindowMinutes ?? 1) * 60;
                var traceId = Guid.NewGuid().ToString("N");
                context.HttpContext.Response.Headers["X-Trace-Id"] = traceId;
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "Too many requests. Please try again later.",
                        type = "RateLimitExceeded",
                        statusCode = 429,
                        traceId,
                        retryAfterSeconds
                    }
                }, cancellationToken);
            };
        });

        return services;
    }
}