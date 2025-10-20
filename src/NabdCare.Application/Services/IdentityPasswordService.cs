using Microsoft.AspNetCore.Identity;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Services;

public class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher;

    public IdentityPasswordService()
    {
        _hasher = new PasswordHasher<User>();
    }

    /// <summary>
    /// Hashes the given password using ASP.NET Core Identity's PasswordHasher.
    /// </summary>
    /// <param name="user">The user entity</param>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password string</returns>
    public string HashPassword(User user, string password)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        return _hasher.HashPassword(user, password);
    }

    /// <summary>
    /// Verifies a plaintext password against the user's stored hashed password.
    /// </summary>
    /// <param name="user">The user entity with PasswordHash property</param>
    /// <param name="password">Plain text password to verify</param>
    /// <returns>True if password is valid, false otherwise</returns>
    public bool VerifyPassword(User user, string password)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return false;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        return result != PasswordVerificationResult.Failed;
    }
}