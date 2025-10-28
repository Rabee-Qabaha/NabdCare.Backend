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
        email = email.Trim().ToLower();

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null) return null;
        if (!user.IsActive && user.Role.Name != "SuperAdmin") return null;
        if (!_passwordService.VerifyPassword(user, password)) return null;

        return user;
    }

    public async Task<User?> AuthenticateUserByIdAsync(Guid userId)
    {
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
        token.ClinicId = user.ClinicId; // null = SuperAdmin

        await _dbContext.RefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => _dbContext.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.Token == token &&
            !rt.IsRevoked &&
            rt.ExpiresAt > DateTime.UtcNow);

    public Task<RefreshToken?> GetRefreshTokenIncludingRevokedAsync(string token)
        => _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

    public async Task RevokeRefreshTokenAsync(string token, string revokedByIp, string reason)
    {
        var rt = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt2 => rt2.Token == token && !rt2.IsRevoked);

        if (rt == null) return;

        rt.IsRevoked = true;
        rt.RevokedAt = DateTime.UtcNow;
        rt.RevokedByIp = revokedByIp;
        rt.ReasonRevoked = reason;

        _dbContext.RefreshTokens.Update(rt);
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarkRefreshTokenReplacedAsync(string oldToken, string newToken, string ip)
    {
        var rt = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == oldToken);

        if (rt == null) return;
        rt.ReplacedByToken = newToken;
        rt.IsRevoked = true;
        rt.RevokedAt = DateTime.UtcNow;
        rt.RevokedByIp = ip;
        rt.ReasonRevoked = "ReplacedByNewToken";

        _dbContext.RefreshTokens.Update(rt);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeTokenFamilyAsync(RefreshToken start)
    {
        var current = start;
        var chain = new List<RefreshToken>();

        while (current != null)
        {
            chain.Add(current);
            if (string.IsNullOrWhiteSpace(current.ReplacedByToken)) break;
            current = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == current.ReplacedByToken);
        }

        var now = DateTime.UtcNow;
        foreach (var rt in chain.Where(t => !t.IsRevoked))
        {
            rt.IsRevoked = true;
            rt.RevokedAt = now;
            rt.RevokedByIp = start.RevokedByIp ?? "System:ReuseProtection";
            rt.ReasonRevoked = "TokenFamilyRevoked";
        }

        _dbContext.RefreshTokens.UpdateRange(chain);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string ip, string reason)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
            t.RevokedByIp = ip;
            t.ReasonRevoked = reason;
        }

        _dbContext.RefreshTokens.UpdateRange(tokens);
        await _dbContext.SaveChangesAsync();
    }
}
