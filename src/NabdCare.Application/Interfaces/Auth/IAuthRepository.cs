using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Interfaces.Auth;
/// <summary>
/// Repository interface for authentication and refresh token management.
/// </summary>
public interface IAuthRepository
{
    /// <summary>
    /// Authenticates a user by email and password. Returns the user if valid, otherwise null.
    /// </summary>
    Task<User?> AuthenticateUserAsync(string email, string password);

    /// <summary>
    /// Authenticates a user by Id. Returns the user if valid, otherwise null.
    /// </summary>
    Task<User?> AuthenticateUserByIdAsync(Guid userId);
    
    /// <summary>
    /// Changes the user's password, hashing it before persisting.
    /// </summary>
    Task ChangePasswordAsync(User user, string newPassword);

    /// <summary>
    /// Saves a refresh token for the user.
    /// </summary>
    Task SaveRefreshTokenAsync(User user, RefreshToken token);

    /// <summary>
    /// Retrieves a refresh token by token string.
    /// </summary>
    Task<RefreshToken?> GetRefreshTokenAsync(string token);

    /// <summary>
    /// Revokes (deletes or marks as invalid) a refresh token.
    /// </summary>
    Task RevokeRefreshTokenAsync(string token);
}