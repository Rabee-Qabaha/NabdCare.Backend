using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces.Auth;
/// <summary>
/// Repository interface for authentication and refresh token management.
/// </summary>
public interface IAuthRepository
{
    Task<User?> AuthenticateUserAsync(string email, string password);
    Task<User?> AuthenticateUserByIdAsync(Guid userId);

    Task ChangePasswordAsync(User user, string newPassword);

    Task SaveRefreshTokenAsync(User user, RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken?> GetRefreshTokenIncludingRevokedAsync(string token);

    Task RevokeRefreshTokenAsync(string token, string revokedByIp, string reason);
    Task MarkRefreshTokenReplacedAsync(string oldToken, string newToken, string revokedByIp);
    Task RevokeTokenFamilyAsync(RefreshToken token);
    Task RevokeAllUserTokensAsync(Guid userId, string revokedByIp, string reason);
}
