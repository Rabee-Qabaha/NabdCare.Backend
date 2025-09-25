using Microsoft.AspNetCore.Identity;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.User;

namespace NabdCare.Application.Services;

public class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<User>? _hasher;

    public IdentityPasswordService()
    {
        _hasher = new PasswordHasher<User>();
    }

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(null /* or a dummy user instance if needed */, password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null, hashedPassword, password);
        return result != PasswordVerificationResult.Failed;
    }
}