using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Auth;

public class AuthRepository : IAuthRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;

    public AuthRepository(NabdCareDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
    }

    public async Task<User?> AuthenticateUserAsync(string email, string password)
    {
        // Normalize the email
        email = email.Trim().ToLower();

        // ✅ FIXED: Include Role when fetching user
        var user = await _dbContext.Users
            .Include(u => u.Role) // Load role relationship
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
            return null;

        // ✅ FIXED: Allow SuperAdmin login even if inactive, otherwise require active
        if (!user.IsActive && user.Role.Name != "SuperAdmin")
            return null;

        // Validate password
        if (!_passwordService.VerifyPassword(user, password))
            return null;

        return user;
    }
    
    public async Task<User?> AuthenticateUserByIdAsync(Guid userId)
    {
        // ✅ FIXED: Include Role when fetching user
        return await _dbContext.Users
            .Include(u => u.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task ChangePasswordAsync(User user, string newPassword)
    {
        user.PasswordHash = _passwordService.HashPassword(user, newPassword);
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
        var toRevoke = await _dbContext.RefreshTokens
            .Where(rt =>
                rt.UserId == token.UserId &&
                (rt.Token == token.Token ||
                    rt.ReplacedByToken == token.Token ||
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
    
    public async Task RevokeAllUserTokensAsync(Guid userId, string revokedByIp, string reason)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
            token.ReasonRevoked = reason;
        }

        _dbContext.RefreshTokens.UpdateRange(tokens);
        await _dbContext.SaveChangesAsync();
    }
}