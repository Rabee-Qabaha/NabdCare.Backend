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
                _logger.LogWarning("Login failed from IP {IP}", requestIp);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // ✅ OPTIONAL: Revoke all existing refresh tokens for this user
            // Might annoy users with multiple devices
            await _authRepository.RevokeAllUserTokensAsync(user.Id, requestIp, "New login");

            var accessToken = _tokenService.GenerateToken(
                user.Id.ToString(), 
                user.Email, 
                user.Role.ToString(), 
                user.ClinicId, 
                user.FullName
            );

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
            _logger.LogInformation("User {UserId} logged in successfully from IP {IP}", user.Id, requestIp);

            return (accessToken, refreshToken.Token);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login from IP {IP}", requestIp);
            throw new InvalidOperationException("An error occurred during login. Please try again later.");
        }
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue, string requestIp)
    {
        try
        {
            var refreshToken = await _authRepository.GetRefreshTokenIncludingRevokedAsync(refreshTokenValue);
            
            if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token from IP {IP}", requestIp);
                throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
            }

            // Token reuse detection with family revocation
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("⚠️ Token reuse detected for user {UserId} from IP {IP} - Revoking token family", 
                    refreshToken.UserId, requestIp);
                await _authRepository.RevokeTokenFamilyAsync(refreshToken);
                throw new UnauthorizedAccessException("Token reuse detected. All tokens have been revoked for security.");
            }

            var user = await _authRepository.AuthenticateUserByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh attempted for inactive user {UserId}", refreshToken.UserId);
                throw new UnauthorizedAccessException("User is not active.");
            }

            // Revoke old token (rotation)
            await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, requestIp, "Rotated");

            //  Create new token with parent tracking
            var newToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = requestIp,
                IsRevoked = false,
                ReplacedByToken = refreshToken.Token
            };

            await _authRepository.SaveRefreshTokenAsync(user, newToken);

            var accessToken = _tokenService.GenerateToken(
                user.Id.ToString(), 
                user.Email, 
                user.Role.ToString(), 
                user.ClinicId, 
                user.FullName
            );
            
            _logger.LogInformation("Refresh token rotated for user {UserId} from IP {IP}", user.Id, requestIp);

            return (accessToken, newToken.Token);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw auth exceptions
            throw;
        }
        catch (Exception ex)
        {
            // Log but throw generic error
            _logger.LogError(ex, "Unexpected error during token refresh from IP {IP}", requestIp);
            throw new InvalidOperationException("An error occurred during token refresh. Please login again.");
        }
    }

    public async Task LogoutAsync(string refreshTokenValue, string requestIp)
    {
        try
        {
            await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, requestIp, "Logout");
            _logger.LogInformation("Logout successful from IP {IP}", requestIp);
        }
        catch (Exception ex)
        {
            // Log error but don't throw (logout should always succeed from client perspective)
            _logger.LogError(ex, "Error during logout from IP {IP} - ignoring", requestIp);
        }
    }
}