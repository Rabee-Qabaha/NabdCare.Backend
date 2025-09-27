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
    /// User is included in case hashing strategy uses per-user data in the future.
    /// </summary>
    public string HashPassword(User user, string password)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        return _hasher.HashPassword(user, password);
    }

    /// <summary>
    /// Verifies a plaintext password against a stored hashed password.
    /// Returns true if valid, false otherwise.
    /// </summary>
    public bool VerifyPassword(string password, string hashed)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashed))
            return false;

        // We don’t have the actual user instance, so we pass null.
        // This is safe because PasswordHasher does not require User for verification
        // unless advanced strategies are configured.
        var result = _hasher.VerifyHashedPassword(null!, hashed, password);

        return result != PasswordVerificationResult.Failed;
    }
}