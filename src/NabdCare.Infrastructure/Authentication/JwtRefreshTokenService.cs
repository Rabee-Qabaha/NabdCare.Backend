using System.Security.Cryptography;
using NabdCare.Domain.Entities.User;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Authentication;

public class JwtRefreshTokenService
{
    private readonly NabdCareDbContext _dbContext;

    public JwtRefreshTokenService(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        _dbContext.SaveChanges();

        return refreshToken;
    }

    public bool ValidateRefreshToken(Guid userId, string token)
    {
        var refreshToken = _dbContext.RefreshTokens
            .FirstOrDefault(r => r.UserId == userId && r.Token == token && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow);

        return refreshToken != null;
    }

    public void RevokeRefreshToken(Guid userId, string token)
    {
        var refreshToken = _dbContext.RefreshTokens
            .FirstOrDefault(r => r.UserId == userId && r.Token == token);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            _dbContext.SaveChanges();
        }
    }
}