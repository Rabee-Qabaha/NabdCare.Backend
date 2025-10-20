using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces;

public interface IPasswordService
{
    /// <summary>
    /// Hashes a password for a specific user
    /// </summary>
    /// <param name="user">The user entity</param>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(User user, string password);

    /// <summary>
    /// Verifies a plain text password against the user's stored hash
    /// </summary>
    /// <param name="user">The user entity with stored PasswordHash</param>
    /// <param name="password">Plain text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(User user, string password);
}