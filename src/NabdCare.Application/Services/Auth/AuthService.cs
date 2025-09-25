using System.Security.Cryptography;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IAuthRepository authRepository, ITokenService tokenService)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
    }

    public async Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password, string requestIp)
    {
        var user = await _authRepository.AuthenticateUserAsync(email, password);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var accessToken = _tokenService.GenerateToken(
            user.Id.ToString(), user.Email, user.Role.ToString(), user.ClinicId
        );

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = requestIp,
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, refreshToken);

        return (accessToken, refreshToken.Token);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue, string requestIp)
    {
        var refreshToken = await _authRepository.GetRefreshTokenIncludingRevokedAsync(refreshTokenValue);

        if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        if (refreshToken.IsRevoked)
        {
            // Token reuse detected
            await _authRepository.RevokeTokenFamilyAsync(refreshToken);
            throw new UnauthorizedAccessException("Refresh token has been revoked. Token reuse detected.");
        }

        var user = await _authRepository.AuthenticateUserByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User is not active.");

        // Revoke the old one with metadata
        await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, requestIp, "Rotated");

        // Create new one
        var newToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = requestIp,
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, newToken);

        var accessToken = _tokenService.GenerateToken(
            user.Id.ToString(), user.Email, user.Role.ToString(), user.ClinicId
        );

        return (accessToken, newToken.Token);
    }

    public async Task LogoutAsync(string refreshTokenValue, string requestIp)
    {
        await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, requestIp, "Logout");
    }
}