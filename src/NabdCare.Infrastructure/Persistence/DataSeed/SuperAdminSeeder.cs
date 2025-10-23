using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Seeds the SuperAdmin user and Ramallah Medical Center demo clinic.
/// Creates initial data for testing and development.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 18:46:03 UTC
/// </summary>
public class SuperAdminSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<SuperAdminSeeder> _logger;

    public int Order => 4;

    public SuperAdminSeeder(
        NabdCareDbContext dbContext,
        IPasswordService passwordService,
        ILogger<SuperAdminSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding SuperAdmin user and demo clinic...");

        await SeedSuperAdminUserAsync();
        await SeedDemoClinicAsync();

        _logger.LogInformation("✅ SuperAdmin and demo clinic seeding completed");
    }

    private async Task SeedSuperAdminUserAsync()
    {
        var superAdminEmail = "sadmin@nabd.care";

        var exists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == superAdminEmail);

        if (exists)
        {
            _logger.LogInformation("✅ SuperAdmin user already exists, skipping creation");
            return;
        }

        var superAdminRole = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && r.IsSystemRole);

        if (superAdminRole == null)
        {
            _logger.LogError("❌ SuperAdmin role not found. Ensure RolesSeeder runs first");
            throw new InvalidOperationException("SuperAdmin role must be seeded before creating SuperAdmin user");
        }

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = superAdminEmail,
            FullName = "Super Admin",
            RoleId = superAdminRole.Id,
            IsActive = true,
            ClinicId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System:Seeder",
            IsDeleted = false
        };

        superAdmin.PasswordHash = _passwordService.HashPassword(superAdmin, "Admin@123!");

        _dbContext.Users.Add(superAdmin);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ SuperAdmin created: {Email}", superAdminEmail);
        _logger.LogWarning("⚠️  IMPORTANT: Change default password immediately in production!");
        _logger.LogInformation("   Email: {Email}", superAdminEmail);
        _logger.LogInformation("   Password: Admin@123!");
    }

    private async Task SeedDemoClinicAsync()
    {
        var demoClinicEmail = "info@ramallahmedical.ps";

        var exists = await _dbContext.Clinics
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Email == demoClinicEmail);

        if (exists)
        {
            _logger.LogInformation("✅ Demo clinic already exists, skipping creation");
            return;
        }

        var clinicAdminRole = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Clinic Admin" && r.IsTemplate);

        var doctorRole = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Doctor" && r.IsTemplate);

        if (clinicAdminRole == null || doctorRole == null)
        {
            _logger.LogError("❌ Required roles not found. Ensure RolesSeeder runs first");
            throw new InvalidOperationException("Clinic Admin and Doctor roles must be seeded first");
        }

        // Create clinic
        var clinicId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var clinic = new Clinic
        {
            Id = clinicId,
            Name = "Ramallah Medical Center",
            Email = demoClinicEmail,
            Phone = "+970-2-2987654",
            Address = "Al-Irsal Street, Downtown Ramallah, Palestine",
            Status = SubscriptionStatus.Active,
            BranchCount = 3,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System:Seeder",
            IsDeleted = false,
            Subscriptions = new List<Subscription>
            {
                new()
                {
                    Id = subscriptionId,
                    ClinicId = clinicId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddYears(1),
                    Type = SubscriptionType.Yearly, // ✅ FIXED: Changed from Monthly to Yearly
                    Fee = 12000m,
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System:Seeder",
                    IsDeleted = false
                }
            }
        };

        _dbContext.Clinics.Add(clinic);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ Demo clinic created: {ClinicName}", clinic.Name);

        // Create Clinic Admin user
        await CreateDemoUserAsync(
            clinicId,
            clinicAdminRole.Id,
            "Admin User",
            "admin@ramallahmedical.ps",
            "Admin@123!"
        );

        // Create Doctor user
        await CreateDemoUserAsync(
            clinicId,
            doctorRole.Id,
            "Dr. Ahmad Hassan",
            "doctor@ramallahmedical.ps",
            "Doctor@123!"
        );
    }

    private async Task CreateDemoUserAsync(
        Guid clinicId,
        Guid roleId,
        string fullName,
        string email,
        string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            RoleId = roleId,
            Email = email,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System:Seeder",
            IsDeleted = false
        };

        user.PasswordHash = _passwordService.HashPassword(user, password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ Demo user created: {Email} ({FullName})", email, fullName);
        _logger.LogInformation("   Email: {Email}", email);
        _logger.LogInformation("   Password: {Password}", password);
    }
}