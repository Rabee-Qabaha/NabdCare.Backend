using System.Security.Cryptography;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;

    public AuthService(IAuthRepository authRepository, ITokenService tokenService)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
    }

    public async Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password)
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
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, refreshToken);

        return (accessToken, refreshToken.Token);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue)
    {
        var refreshToken = await _authRepository.GetRefreshTokenAsync(refreshTokenValue);

        if (refreshToken == null)
            throw new UnauthorizedAccessException("Refresh token is invalid, expired, or revoked.");

        var user = await _authRepository.AuthenticateUserByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User is not active.");

        // Revoke the old refresh token and issue a new one for security
        await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, newRefreshToken);

        var accessToken = _tokenService.GenerateToken(
            user.Id.ToString(), user.Email, user.Role.ToString(), user.ClinicId
        );

        return (accessToken, newRefreshToken.Token);
    }

    public async Task LogoutAsync(string refreshTokenValue)
    {
        await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue);
    }
}