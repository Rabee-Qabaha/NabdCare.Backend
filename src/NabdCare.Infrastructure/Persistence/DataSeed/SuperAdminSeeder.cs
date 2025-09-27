using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class SuperAdminSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ITenantContext _tenantContext;

    public SuperAdminSeeder(
        NabdCareDbContext dbContext,
        IPasswordService passwordService,
        ITenantContext tenantContext
    )
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _tenantContext = tenantContext;
    }

    public async Task SeedAsync()
    {
        // Temporarily elevate tenant context
        var prevSuper = _tenantContext.IsSuperAdmin;
        _tenantContext.IsSuperAdmin = true;

        try
        {
            var exists = await _dbContext.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Role == UserRole.SuperAdmin && u.IsActive);

            if (!exists)
            {
                // Create a new User object first
                var superAdmin = new User
                {
                    Email = "sadmin@nabd.care",
                    FullName = "Super Admin",
                    Role = UserRole.SuperAdmin,
                    IsActive = true
                };

                // Hash the password using the user instance
                superAdmin.PasswordHash = _passwordService.HashPassword(superAdmin, "Admin@123!");

                _dbContext.Users.Add(superAdmin);
                await _dbContext.SaveChangesAsync();
            }
        }
        finally
        {
            _tenantContext.IsSuperAdmin = prevSuper;
        }
    }
}