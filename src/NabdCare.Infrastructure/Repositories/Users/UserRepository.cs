using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Users;

public class UserRepository : IUserRepository
{
    private readonly NabdCareDbContext _dbContext;

    public UserRepository(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IEnumerable<User>> GetUsersByClinicIdAsync(Guid? clinicId)
    {
        return await _dbContext.Users
            .Where(u => !clinicId.HasValue || u.ClinicId == clinicId)
            .ToListAsync();
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteUserAsync(Guid userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) return false;

        user.IsDeleted = true;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

}