using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class DbSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ITenantContext _tenantContext;

    public DbSeeder(
        NabdCareDbContext dbContext,
        IPasswordService passwordService,
        ITenantContext tenantContext
    )
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _tenantContext = tenantContext;
    }

    public void Seed()
    {
        // Elevate tenant context temporarily
        var prevSuper = _tenantContext.IsSuperAdmin;
        _tenantContext.IsSuperAdmin = true;
        try
        {
            _dbContext.Database.Migrate();

            if (!_dbContext.Users
                    .IgnoreQueryFilters()
                    .Any(u => u.Role == UserRole.SuperAdmin && u.IsActive))
            {
                var password = "Admin@123!";
                var passwordHash = _passwordService.HashPassword(password);

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
        finally
        {
            _tenantContext.IsSuperAdmin = prevSuper;
        }
    }
}