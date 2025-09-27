using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Application.Interfaces.Auth;

namespace NabdCare.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth").WithTags("Authentication");

        // Login
        authGroup.MapPost("/login", async ([FromBody] LoginRequest req, [FromServices] IAuthService authService, HttpContext http) =>
        {
            var ip = GetClientIp(http);
            var (accessToken, refreshToken) = await authService.LoginAsync(req.Email, req.Password, ip);
            return Results.Ok(new { accessToken, refreshToken });
        })
        .WithName("Login")
        .WithOpenApi();

        // Refresh token
        authGroup.MapPost("/refresh", async ([FromBody] RefreshRequest req, [FromServices] IAuthService authService, HttpContext http) =>
        {
            var ip = GetClientIp(http);
            var (accessToken, refreshToken) = await authService.RefreshTokenAsync(req.RefreshToken, ip);
            return Results.Ok(new { accessToken, refreshToken });
        })
        .WithName("RefreshToken")
        .WithOpenApi();

        // Logout
        authGroup.MapPost("/logout", async ([FromBody] RefreshRequest req, [FromServices] IAuthService authService, HttpContext http) =>
        {
            var ip = GetClientIp(http);
            await authService.LogoutAsync(req.RefreshToken, ip);
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

        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}