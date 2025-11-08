using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
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
        _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password, string ip)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException($"Email cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(email));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException($"Password cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(password));

        if (string.IsNullOrWhiteSpace(ip))
            throw new ArgumentException($"IP address cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(ip));

        var normalizedEmail = email.Trim().ToLower();
        _logger.LogInformation("User login attempt from IP {IP} with email {Email}", ip, normalizedEmail);

        var user = await _authRepository.AuthenticateUserAsync(normalizedEmail, password);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for email {Email} from IP {IP}. Error code: {ErrorCode}",
                normalizedEmail, ip, ErrorCodes.UNAUTHORIZED);
            throw new UnauthorizedAccessException($"Invalid credentials. Error code: {ErrorCodes.UNAUTHORIZED}");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user {UserId} ({Email}) from IP {IP}. Error code: {ErrorCode}",
                user.Id, normalizedEmail, ip, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"User account is inactive. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("User {UserId} ({Email}) authenticated successfully from IP {IP}", user.Id, normalizedEmail, ip);

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

        _logger.LogInformation("Refresh token saved for user {UserId}. Expires: {ExpiryDate}", user.Id, refreshToken.ExpiresAt);

        // Preload permissions into cache
        try
        {
            await _permissionService.GetUserEffectivePermissionsAsync(user.Id, user.RoleId);
            _logger.LogInformation("Preloaded permission cache for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload permission cache for user {UserId}. Error code: {ErrorCode}",
                user.Id, ErrorCodes.INTERNAL_ERROR);
            // Don't throw - permission cache is optional warmup
        }

        return (accessToken, refreshToken.Token);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string oldToken, string ip)
    {
        if (string.IsNullOrWhiteSpace(oldToken))
            throw new ArgumentException($"Refresh token cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(oldToken));

        if (string.IsNullOrWhiteSpace(ip))
            throw new ArgumentException($"IP address cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(ip));

        var normalizedToken = oldToken.Trim();
        _logger.LogDebug("Token refresh attempt from IP {IP}", ip);

        var token = await _authRepository.GetRefreshTokenIncludingRevokedAsync(normalizedToken);
        if (token == null)
        {
            _logger.LogWarning("Token refresh failed: Token not found. IP: {IP}. Error code: {ErrorCode}",
                ip, ErrorCodes.UNAUTHORIZED);
            throw new UnauthorizedAccessException($"Invalid refresh token. Error code: {ErrorCodes.UNAUTHORIZED}");
        }

        // Check if token expired
        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: Token expired. User: {UserId}, IP: {IP}. Error code: {ErrorCode}",
                token.UserId, ip, ErrorCodes.UNAUTHORIZED);

            await _authRepository.RevokeRefreshTokenAsync(normalizedToken, ip, "Expired");
            throw new UnauthorizedAccessException($"Refresh token expired. Error code: {ErrorCodes.UNAUTHORIZED}");
        }

        // Check if token was revoked (security breach detection)
        if (token.IsRevoked)
        {
            _logger.LogError("SECURITY: Token reuse detected. User: {UserId}, IP: {IP}. Error code: {ErrorCode}",
                token.UserId, ip, ErrorCodes.SECURITY_VIOLATION);

            await _authRepository.RevokeTokenFamilyAsync(token);
            throw new UnauthorizedAccessException($"Token reuse detected. Login required. Error code: {ErrorCodes.SECURITY_VIOLATION}");
        }

        var user = await _authRepository.AuthenticateUserByIdAsync(token.UserId);
        if (user == null)
        {
            _logger.LogWarning("Token refresh failed: User not found. UserId: {UserId}, IP: {IP}. Error code: {ErrorCode}",
                token.UserId, ip, ErrorCodes.USER_NOT_FOUND);
            throw new UnauthorizedAccessException($"User not found or inactive. Error code: {ErrorCodes.USER_NOT_FOUND}");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Token refresh failed: User inactive. UserId: {UserId}, IP: {IP}. Error code: {ErrorCode}",
                user.Id, ip, ErrorCodes.FORBIDDEN);
            throw new UnauthorizedAccessException($"User account is inactive. Error code: {ErrorCodes.FORBIDDEN}");
        }

        _logger.LogInformation("User {UserId} refreshing token from IP {IP}", user.Id, ip);

        // Generate new refresh token
        var newTokenValue = _tokenService.GenerateRefreshToken();
        var newToken = new RefreshToken
        {
            UserId = user.Id,
            Token = newTokenValue,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            IsRevoked = false
        };

        await _authRepository.SaveRefreshTokenAsync(user, newToken);

        _logger.LogDebug("New refresh token created for user {UserId}. Expires: {ExpiryDate}", user.Id, newToken.ExpiresAt);

        // Mark old token as replaced
        await _authRepository.MarkRefreshTokenReplacedAsync(normalizedToken, newTokenValue, ip);

        _logger.LogInformation("Old token marked as replaced for user {UserId}", user.Id);

        // Generate new access token
        var accessToken = _tokenService.GenerateToken(
            user.Id.ToString(), user.Email, user.Role.Name,
            user.RoleId, user.ClinicId, user.FullName);

        _logger.LogInformation("New access token generated for user {UserId}", user.Id);

        // Warm up permission cache (in case permissions changed)
        try
        {
            await _permissionService.GetUserEffectivePermissionsAsync(user.Id, user.RoleId);
            _logger.LogInformation("Refreshed permission cache for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh permission cache for user {UserId}. Error code: {ErrorCode}",
                user.Id, ErrorCodes.INTERNAL_ERROR);
            // Don't throw - permission cache is optional warmup
        }

        return (accessToken, newToken.Token);
    }

    public async Task LogoutAsync(string refreshTokenValue, string ip)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
            throw new ArgumentException($"Refresh token cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(refreshTokenValue));

        if (string.IsNullOrWhiteSpace(ip))
            throw new ArgumentException($"IP address cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(ip));

        var normalizedToken = refreshTokenValue.Trim();
        _logger.LogInformation("Logout initiated from IP {IP}", ip);

        await _authRepository.RevokeRefreshTokenAsync(normalizedToken, ip, "Logout");

        _logger.LogInformation("User logged out successfully from IP {IP}", ip);
    }
}