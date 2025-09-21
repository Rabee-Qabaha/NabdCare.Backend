using Microsoft.EntityFrameworkCore;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Helpers;

namespace NabdCare.Infrastructure.Persistence;

public class DbSeeder
{
    private readonly NabdCareDbContext _dbContext;

    public DbSeeder(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Seed()
    {
        _dbContext.Database.Migrate();

        if (!_dbContext.Users.Any(u => u.Role == UserRole.SuperAdmin && u.IsActive))
        {
            var password = "Admin@123!";
            var passwordHash = PasswordHelper.HashPassword(password);

            var superAdmin = new User
            {
                Email = "sadmin@nabd.care",
                FullName = "Super Admin",
                PasswordHash = passwordHash,
                Role = UserRole.SuperAdmin,
                IsActive = true
            };

            _dbContext.Users.Add(superAdmin);
            _dbContext.SaveChanges();
        }
    }
}