using System.Net;
using Microsoft.AspNetCore.Identity.Data;
using NabdCare.Application.Interfaces.Auth;

namespace NabdCare.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth");

        authGroup.MapPost("/login", async (LoginRequest req, IAuthService authService, HttpContext http) =>
        {
            var ip = GetClientIp(http);
            var (accessToken, refreshToken) = await authService.LoginAsync(req.Email, req.Password, ip);
            return Results.Ok(new { accessToken, refreshToken });
        });

        authGroup.MapPost("/refresh", async (RefreshRequest req, IAuthService authService, HttpContext http) =>
        {
            var ip = GetClientIp(http);
            var (accessToken, newRefreshToken) = await authService.RefreshTokenAsync(req.RefreshToken, ip);
            return Results.Ok(new { accessToken, refreshToken = newRefreshToken });
        });

        authGroup.MapPost("/logout", async (RefreshRequest req, IAuthService authService, HttpContext http) =>
        {
            var ip = GetClientIp(http);
            await authService.LogoutAsync(req.RefreshToken, ip);
            return Results.NoContent();
        });
    }

    private static string GetClientIp(HttpContext http)
    {
        // Check X-Forwarded-For header first
        if (http.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var first = forwardedFor.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                // Sometimes X-Forwarded-For contains multiple addresses separated by ','
                var ip = first.Split(',', StringSplitOptions.RemoveEmptyEntries).First().Trim();
                if (IPAddress.TryParse(ip, out var parsed))
                    return ip;
            }
        }

        // Fallback to remote IP
        var remoteIp = http.Connection.RemoteIpAddress;
        if (remoteIp != null) return remoteIp.ToString();

        return "unknown";
    }
}