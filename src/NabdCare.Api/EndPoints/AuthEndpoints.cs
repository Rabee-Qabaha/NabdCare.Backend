using NabdCare.Application.Interfaces.Auth;

namespace NabdCare.Api.EndPoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
            {
                var (accessToken, refreshToken) = await authService.LoginAsync(request.Email, request.Password);
                return Results.Ok(new AuthResponse(accessToken, refreshToken));
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Authenticate user and get tokens");

        app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
            {
                var (accessToken, refreshToken) = await authService.RefreshTokenAsync(request.RefreshToken);
                return Results.Ok(new AuthResponse(accessToken, refreshToken));
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Refresh access token");

        app.MapPost("/api/auth/logout", async (RefreshTokenRequest request, IAuthService authService) =>
            {
                await authService.LogoutAsync(request.RefreshToken);
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Logout and revoke refresh token");
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken);