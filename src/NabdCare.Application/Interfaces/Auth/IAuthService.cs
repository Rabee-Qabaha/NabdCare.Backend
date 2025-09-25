using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Interfaces.Auth;

public interface IAuthService
{
    /// <summary>
    /// Logs in a user with email & password.
    /// Returns access token + refresh token.
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password (plain)</param>
    /// <param name="requestIp">IP address of the request (for audit)</param>
    Task<(string accessToken, string refreshToken)> LoginAsync(string email, string password, string requestIp);

    /// <summary>
    /// Refreshes tokens using a refresh token string.
    /// Implements rotation: revokes old, issues new.
    /// </summary>
    /// <param name="refreshToken">The old refresh token value</param>
    /// <param name="requestIp">IP address of the request (for audit)</param>
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken, string requestIp);

    /// <summary>
    /// Logs out (revokes) the given refresh token.
    /// </summary>
    /// <param name="refreshToken">Token to revoke</param>
    /// <param name="requestIp">IP address of the request (for audit)</param>
    Task LogoutAsync(string refreshToken, string requestIp);
}