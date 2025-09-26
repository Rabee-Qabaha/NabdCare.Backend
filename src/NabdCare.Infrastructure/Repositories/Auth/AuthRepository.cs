using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Domain.Entities.User;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Auth;

public class AuthRepository : IAuthRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;  // assuming you have abstracted

    public AuthRepository(NabdCareDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<User?> AuthenticateUserAsync(string email, string password)
    {
        User? user;
        if (email.Trim().ToLower() == "sadmin@nabd.care")
        {
            user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower() && u.IsActive);
        }
        else
        {
            user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower() && u.IsActive);
        }

        if (user == null)
            return null;

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
            return null;

        return user;
    }

    public async Task<User?> AuthenticateUserByIdAsync(Guid userId)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task ChangePasswordAsync(User user, string newPassword)
    {
        user.PasswordHash = _passwordService.HashPassword(newPassword);
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveRefreshTokenAsync(User user, RefreshToken token)
    {
        token.UserId = user.Id;
        await _dbContext.RefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
    }

    // New method: get including revoked
    public async Task<RefreshToken?> GetRefreshTokenIncludingRevokedAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeRefreshTokenAsync(string token, string revokedByIp, string reason)
    {
        var rt = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt2 => rt2.Token == token && !rt2.IsRevoked);

        if (rt != null)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = revokedByIp;
            rt.ReasonRevoked = reason;

            _dbContext.RefreshTokens.Update(rt);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RevokeTokenFamilyAsync(RefreshToken token)
    {
        // Revoke this token and all tokens in its "family" i.e. those that were replaced by it or descendants
        // Simple approach: tokens that have this token in their ReplacedByToken chain
        var toRevoke = await _dbContext.RefreshTokens
            .Where(rt =>
                rt.UserId == token.UserId &&
                (rt.Token == token.Token ||
                    rt.ReplacedByToken == token.Token ||
                    // optionally more complex chain via multiple levels
                    false
                ) &&
                !rt.IsRevoked)
            .ToListAsync();

        foreach (var rt in toRevoke)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = token.RevokedByIp;
            rt.ReasonRevoked = "TokenFamilyRevoked";
        }

        _dbContext.RefreshTokens.UpdateRange(toRevoke);
        await _dbContext.SaveChangesAsync();
    }
}