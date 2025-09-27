using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    private const int RefreshTokenDays = 7;

    public AuthService(IAuthRepository authRepository, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password, string requestIp)
    {
        try
        {
            var user = await _authRepository.AuthenticateUserAsync(email, password);
            if (user == null)
            {
                _logger.LogWarning("Login failed for email {Email}", email);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var accessToken = _tokenService.GenerateToken(user.Id.ToString(), user.Email, user.Role.ToString(), user.ClinicId);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = requestIp,
                IsRevoked = false
            };

            await _authRepository.SaveRefreshTokenAsync(user, refreshToken);
            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return (accessToken, refreshToken.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}", email);
            throw;
        }
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue, string requestIp)
    {
        try
        {
            var refreshToken = await _authRepository.GetRefreshTokenIncludingRevokedAsync(refreshTokenValue);
            if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

            if (refreshToken.IsRevoked)
            {
                await _authRepository.RevokeTokenFamilyAsync(refreshToken);
                _logger.LogWarning("Token reuse detected for user {UserId}", refreshToken.UserId);
                throw new UnauthorizedAccessException("Refresh token has been revoked. Token reuse detected.");
            }

            var user = await _authRepository.AuthenticateUserByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User is not active.");

            await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, requestIp, "Rotated");

            var newToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = requestIp,
                IsRevoked = false
            };

            await _authRepository.SaveRefreshTokenAsync(user, newToken);

            var accessToken = _tokenService.GenerateToken(user.Id.ToString(), user.Email, user.Role.ToString(), user.ClinicId);
            _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

            return (accessToken, newToken.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token failed for token {Token}", refreshTokenValue);
            throw;
        }
    }

    public async Task LogoutAsync(string refreshTokenValue, string requestIp)
    {
        try
        {
            await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, requestIp, "Logout");
            _logger.LogInformation("Logout successful for token {Token}", refreshTokenValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed for token {Token}", refreshTokenValue);
            throw;
        }
    }
}