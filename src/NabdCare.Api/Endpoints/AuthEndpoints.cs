using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.Auth;
using NabdCare.Application.Interfaces.Auth;

namespace NabdCare.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/auth").WithTags("Authentication");

        authGroup.MapPost("/login", async (
            [FromBody] LoginRequestDto req,
            [FromServices] IAuthService authService,
            HttpContext http) =>
        {
            // ✅ Basic validation (FluentValidation handles detailed validation)
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        message = "Email and Password are required.",
                        type = "ValidationError",
                        statusCode = 400
                    }
                });
            }

            var ip = GetClientIp(http);
            
            var (accessToken, refreshToken) = await authService.LoginAsync(req.Email, req.Password, ip);

            // Set refresh token as httpOnly cookie
            http.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Results.Ok(new AuthResponseDto(accessToken));
        })
        .WithName("Login")
        .WithOpenApi()
        .RequireRateLimiting("auth"); // ✅ Apply rate limiting

        authGroup.MapPost("/refresh", async (
            [FromServices] IAuthService authService,
            HttpContext http) =>
        {
            if (!http.Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                return Results.Json(
                    new
                    {
                        error = new
                        {
                            message = "Refresh token not found.",
                            type = "UnauthorizedAccess",
                            statusCode = 401
                        }
                    },
                    statusCode: 401
                );
            }

            var ip = GetClientIp(http);
            
            var (newAccessToken, newRefreshToken) = await authService.RefreshTokenAsync(refreshToken, ip);

            // Rotate cookie
            http.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Results.Ok(new AuthResponseDto(newAccessToken));
        })
        .WithName("RefreshToken")
        .WithOpenApi()
        .RequireRateLimiting("auth"); // ✅ Apply rate limiting

        // ✅ LOGOUT (no rate limiting needed)
        authGroup.MapPost("/logout", async (
            [FromServices] IAuthService authService,
            HttpContext http) =>
        {
            if (http.Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                var ip = GetClientIp(http);
                await authService.LogoutAsync(refreshToken, ip);
                http.Response.Cookies.Delete("refreshToken");
            }

            return Results.NoContent();
        })
        .WithName("Logout")
        .WithOpenApi();
    }

    private static string GetClientIp(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var first = forwardedFor.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                var ip = first.Split(',', StringSplitOptions.RemoveEmptyEntries).First().Trim();
                if (System.Net.IPAddress.TryParse(ip, out _))
                    return ip;
            }
        }

        // Fallback to X-Real-IP header (common in nginx)
        if (http.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            var ip = realIp.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip) && System.Net.IPAddress.TryParse(ip, out _))
                return ip;
        }

        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}