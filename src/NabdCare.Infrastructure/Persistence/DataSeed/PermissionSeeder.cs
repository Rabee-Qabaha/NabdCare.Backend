using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class PermissionsSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<PermissionsSeeder> _logger;

    public int Order => 2; // Run after SuperAdminSeeder

    public PermissionsSeeder(
        NabdCareDbContext dbContext,
        ILogger<PermissionsSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding permissions...");

        var permissions = new[]
        {
            new AppPermission { Name = "ViewUsers", Description = "View users list" },
            new AppPermission { Name = "CreateUser", Description = "Create new users" },
            new AppPermission { Name = "UpdateUser", Description = "Update user details" },
            new AppPermission { Name = "DeleteUser", Description = "Delete users" },
            new AppPermission { Name = "UpdateUserRole", Description = "Change user roles" },
            new AppPermission { Name = "ResetPassword", Description = "Reset user passwords" },
            new AppPermission { Name = "AdminResetPassword", Description = "SuperAdmin password reset" },
            new AppPermission { Name = "ViewPatients", Description = "View patients list" },
            new AppPermission { Name = "CreatePatient", Description = "Create new patients" },
            new AppPermission { Name = "UpdatePatient", Description = "Update patient details" },
            new AppPermission { Name = "DeletePatient", Description = "Delete patients" },
        };

        foreach (var permission in permissions)
        {
            var exists = await _dbContext.Set<AppPermission>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Name == permission.Name);

            if (!exists)
            {
                permission.Id = Guid.NewGuid();
                permission.CreatedAt = DateTime.UtcNow;
                _dbContext.Set<AppPermission>().Add(permission);
                _logger.LogInformation("   ➕ Added permission: {Name}", permission.Name);
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("✅ Permissions seeded successfully.");
    }
}