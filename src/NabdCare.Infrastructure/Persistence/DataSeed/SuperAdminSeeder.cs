using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class SuperAdminSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<SuperAdminSeeder> _logger;

    // ✅ Add Order property
    public int Order => 1; // Run first

    public SuperAdminSeeder(
        NabdCareDbContext dbContext,
        IPasswordService passwordService,
        ILogger<SuperAdminSeeder> logger
    )
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding SuperAdmin user...");

        // Check if SuperAdmin already exists
        var exists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == "sadmin@nabd.care");

        if (exists)
        {
            _logger.LogInformation("✅ SuperAdmin user already exists, skipping seed.");
            return;
        }

        // Create SuperAdmin user
        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = "sadmin@nabd.care",
            FullName = "Super Admin",
            Role = UserRole.SuperAdmin,
            IsActive = true,
            ClinicId = null, // SuperAdmin doesn't belong to a clinic
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Hash the password
        superAdmin.PasswordHash = _passwordService.HashPassword(superAdmin, "Admin@123!");

        _dbContext.Users.Add(superAdmin);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ SuperAdmin created successfully: {Email}", superAdmin.Email);
        _logger.LogWarning("⚠️  IMPORTANT: Change default password immediately!");
        _logger.LogInformation("   Email: {Email}", superAdmin.Email);
        _logger.LogInformation("   Password: Admin@123!");
    }
}