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
/// Updated: 2025-10-23 21:00:21 UTC
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

        try
        {
            await SeedSuperAdminUserAsync();
            await SeedDemoClinicAsync();
            
            _logger.LogInformation("✅ SuperAdmin and demo clinic seeding completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SuperAdmin seeding failed: {Message}", ex.Message);
            throw;
        }
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

        // Check if clinic exists
        var clinic = await _dbContext.Clinics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Email == demoClinicEmail);

        if (clinic == null)
        {
            _logger.LogInformation("🔄 Creating demo clinic...");
            
            // Create clinic if it doesn't exist
            var clinicId = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();

            clinic = new Clinic
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
                        Type = SubscriptionType.Yearly,
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
        }
        else
        {
            _logger.LogInformation("✅ Demo clinic already exists: {ClinicName}", clinic.Name);
        }

        // Get required roles
        var clinicAdminRole = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Clinic Admin" && r.IsTemplate);

        var doctorRole = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Doctor" && r.IsTemplate);

        if (clinicAdminRole == null || doctorRole == null)
        {
            _logger.LogError("❌ Required roles not found. Clinic Admin: {CAFound}, Doctor: {DFound}", 
                clinicAdminRole != null, doctorRole != null);
            throw new InvalidOperationException("Clinic Admin and Doctor roles must be seeded first");
        }

        _logger.LogInformation("✅ Found roles - Clinic Admin: {CAId}, Doctor: {DId}", 
            clinicAdminRole.Id, doctorRole.Id);

        // Create Clinic Admin if doesn't exist
        var clinicAdminExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == "cadmin@nabd.care");

        if (!clinicAdminExists)
        {
            try
            {
                _logger.LogInformation("🔄 Creating Clinic Admin user...");
                await CreateDemoUserAsync(
                    clinic.Id,
                    clinicAdminRole.Id,
                    "Admin User",
                    "cadmin@nabd.care",
                    "Admin@123!"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create Clinic Admin: {Message}", ex.Message);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("✅ Clinic admin already exists: cadmin@nabd.care");
        }

        // Create Doctor if doesn't exist
        var doctorExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == "dadmin@nabd.care");

        if (!doctorExists)
        {
            try
            {
                _logger.LogInformation("🔄 Creating Doctor user...");
                await CreateDemoUserAsync(
                    clinic.Id,
                    doctorRole.Id,
                    "Dr. Ahmad Hassan",
                    "dadmin@nabd.care",
                    "Doctor@123!"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create Doctor: {Message}", ex.Message);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("✅ Doctor already exists: dadmin@nabd.care");
        }
    }

    private async Task CreateDemoUserAsync(
        Guid clinicId,
        Guid roleId,
        string fullName,
        string email,
        string password)
    {
        try
        {
            _logger.LogInformation("  🔄 Creating user {Email} with ClinicId: {ClinicId}, RoleId: {RoleId}", 
                email, clinicId, roleId);
            
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

            _logger.LogInformation("  🔄 Hashing password for {Email}...", email);
            user.PasswordHash = _passwordService.HashPassword(user, password);
            
            _logger.LogInformation("  🔄 Adding user {Email} to DbContext...", email);
            _dbContext.Users.Add(user);
            
            _logger.LogInformation("  🔄 Saving user {Email} to database...", email);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("  ✅ Demo user created: {Email} ({FullName})", email, fullName);
            _logger.LogInformation("     Email: {Email}", email);
            _logger.LogInformation("     Password: {Password}", password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  ❌ EXCEPTION creating user {Email}: {Message}", email, ex.Message);
            _logger.LogError("  📜 Stack trace: {StackTrace}", ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("  💥 Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
            
            throw;
        }
    }
}