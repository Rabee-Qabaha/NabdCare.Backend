using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthService> _logger;

    private const int RefreshTokenDays = 30;

    public AuthService(
        IAuthRepository authRepository,
        ITokenService tokenService,
        IPermissionService permissionService,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password, string ip)
    {
        var user = await _authRepository.AuthenticateUserAsync(email, password)
                   ?? throw new UnauthorizedAccessException("Invalid credentials");

        var accessToken = _tokenService.GenerateToken(
            user.Id.ToString(), user.Email, user.Role.Name,
            user.RoleId, user.ClinicId, user.FullName);

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, refreshToken);

        // ✅ Warm-up: preload the user's effective permissions into memory cache
        await _permissionService.GetUserEffectivePermissionsAsync(user.Id, user.RoleId);
        _logger.LogInformation("Preloaded permission cache for user {UserId}", user.Id);

        return (accessToken, refreshToken.Token);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string oldToken, string ip)
    {
        var token = await _authRepository.GetRefreshTokenIncludingRevokedAsync(oldToken)
                   ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            await _authRepository.RevokeRefreshTokenAsync(oldToken, ip, "Expired");
            throw new UnauthorizedAccessException("Refresh token expired");
        }

        if (token.IsRevoked)
        {
            await _authRepository.RevokeTokenFamilyAsync(token);
            throw new UnauthorizedAccessException("Token reuse detected. Login required.");
        }

        var user = await _authRepository.AuthenticateUserByIdAsync(token.UserId)
                   ?? throw new UnauthorizedAccessException("User inactive");

        var newValue = _tokenService.GenerateRefreshToken();

        var newToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newValue,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, newToken);
        await _authRepository.MarkRefreshTokenReplacedAsync(oldToken, newValue, ip);

        var accessToken = _tokenService.GenerateToken(
            user.Id.ToString(), user.Email, user.Role.Name,
            user.RoleId, user.ClinicId, user.FullName);

        // ✅ Optional: Warm up again in case roles/permissions changed
        await _permissionService.GetUserEffectivePermissionsAsync(user.Id, user.RoleId);
        _logger.LogInformation("Refreshed permission cache for user {UserId}", user.Id);

        return (accessToken, newToken.Token);
    }

    public async Task LogoutAsync(string refreshTokenValue, string ip)
    {
        await _authRepository.RevokeRefreshTokenAsync(refreshTokenValue, ip, "Logout");
    }
}