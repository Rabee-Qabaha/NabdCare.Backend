using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.DTOs.Auth;
using NabdCare.Application.Interfaces.Auth;

namespace NabdCare.Api.Endpoints;

/// <summary>
/// Authentication endpoints for login, token refresh, and logout.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-26 ✅ Improved cookie security for dev & prod
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("auth").WithTags("Authentication");

        // ============================================
        // LOGIN
        // ============================================
        authGroup.MapPost("login", async (
            [FromBody] LoginRequestDto req,
            [FromServices] IAuthService authService,
            HttpContext http) =>
        {
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

            var isProd = !http.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase);

            http.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                SameSite = isProd ? SameSiteMode.Strict : SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),
                IsEssential = true
            });

            return Results.Ok(new AuthResponseDto(accessToken));
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("User login with email and password")
        .WithOpenApi()
        .RequireRateLimiting("auth");

        // ============================================
        // REFRESH TOKEN
        // ============================================
        authGroup.MapPost("refresh", async (
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

            var isProd = !http.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase);

            http.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                SameSite = isProd ? SameSiteMode.Strict : SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7),
                IsEssential = true
            });

            return Results.Ok(new AuthResponseDto(newAccessToken));
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .WithSummary("Refresh access token using refresh token cookie")
        .WithOpenApi()
        .RequireRateLimiting("auth");

        // ============================================
        // LOGOUT
        // ============================================
        authGroup.MapPost("logout", async (
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
        .RequireAuthorization()
        .WithName("Logout")
        .WithSummary("Logout and revoke refresh token")
        .WithOpenApi();
    }

    private static string GetClientIp(HttpContext http)
    {
        var xfwd = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xfwd))
        {
            var first = xfwd.Split(',').Select(s => s.Trim()).FirstOrDefault();
            if (!string.IsNullOrEmpty(first))
                return first;
        }

        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}