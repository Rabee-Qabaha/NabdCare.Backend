using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Users;

/// <summary>
/// Production-ready user repository - thin data access layer.
/// No try-catch: exceptions bubble up to service layer.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        NabdCareDbContext dbContext,
        ILogger<UserRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
    }

    public async Task<User?> GetByIdRawAsync(Guid id)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByClinicIdAsync(Guid clinicId)
    {
        if (clinicId == Guid.Empty)
            return Enumerable.Empty<User>();

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Where(u => u.ClinicId == clinicId && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    public async Task<bool> ExistsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && !u.IsDeleted);
    }

    #endregion

    #region COMMAND METHODS

    public async Task<User> CreateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        // Normalize email
        user.Email = user.Email.Trim().ToLower();

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created in database", user.Id);

        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        // Normalize email if changed
        if (!string.IsNullOrWhiteSpace(user.Email))
            user.Email = user.Email.Trim().ToLower();

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated in database", user.Id);

        return user;
    }

    public async Task<bool> SoftDeleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
            return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} soft deleted in database", userId);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("User {UserId} PERMANENTLY DELETED from database", userId);

        return true;
    }

    #endregion
}