using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Auth;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Helpers;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Auth;

/// <summary>
    /// Implements authentication and refresh token management using EF Core.
    /// </summary>
    public class AuthRepository : IAuthRepository
    {
        private readonly NabdCareDbContext _dbContext;

        public AuthRepository(NabdCareDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> AuthenticateUserAsync(string email, string password)
        {
            User? user;
            // Always bypass query filters for SuperAdmin
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

            // Secure password validation using PasswordHelper
            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User?> AuthenticateUserByIdAsync(Guid userId)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }
        
        public async Task ChangePasswordAsync(User user, string newPassword)
        {
            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
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
                // .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                _dbContext.RefreshTokens.Update(refreshToken);
                await _dbContext.SaveChangesAsync();
            }
        }
    }