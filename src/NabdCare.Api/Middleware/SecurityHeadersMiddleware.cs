using Microsoft.Extensions.Options;
using NabdCare.Api.Configurations;

namespace NabdCare.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _allowedOrigins;

        public SecurityHeadersMiddleware(RequestDelegate next, IOptions<FrontendSettings> options)
        {
            _next = next;
            _allowedOrigins = options.Value.AllowedOrigins ?? Array.Empty<string>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // 🔒 Basic security headers
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // ✅ Content-Security-Policy with dynamic origins
            var connectSrc = string.Join(" ", _allowedOrigins.Append("'self'"));
            headers["Content-Security-Policy"] =
                $"default-src 'self'; " +
                $"img-src 'self' data: https:; " +
                $"script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                $"style-src 'self' 'unsafe-inline' https:; " +
                $"font-src 'self' https: data:; " +
                $"connect-src {connectSrc}; " +
                $"frame-ancestors 'none'; " +
                $"base-uri 'self';";

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}