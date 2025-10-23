using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Seeds default system and template roles into the database.
/// System roles: SuperAdmin, SupportManager, BillingManager
/// Template roles: Clinic Admin, Doctor, Nurse, Receptionist
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 18:46:03 UTC
/// </summary>
public class RolesSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<RolesSeeder> _logger;

    public int Order => 1;

    public RolesSeeder(
        NabdCareDbContext dbContext,
        ILogger<RolesSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding default roles...");

        var systemRoles = GetSystemRoles();
        var templateRoles = GetTemplateRoles();
        var allRoles = systemRoles.Concat(templateRoles).ToList();
        
        var addedCount = 0;

        foreach (var role in allRoles)
        {
            var exists = await _dbContext.Roles
                .IgnoreQueryFilters()
                .AnyAsync(r => r.Name == role.Name && r.ClinicId == null);

            if (!exists)
            {
                role.Id = Guid.NewGuid();
                role.CreatedAt = DateTime.UtcNow;
                role.CreatedBy = "System:Seeder";
                role.IsDeleted = false;

                _dbContext.Roles.Add(role);
                addedCount++;

                var roleType = role.IsSystemRole ? "System" : "Template";
                _logger.LogDebug("   ➕ Added role: {Name} ({Type})", role.Name, roleType);
            }
        }

        if (addedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ {Count} roles seeded successfully", addedCount);
        }
        else
        {
            _logger.LogInformation("✅ All roles already exist, skipping seed");
        }
    }

    private static List<Role> GetSystemRoles()
    {
        return new List<Role>
        {
            new()
            {
                Name = "SuperAdmin",
                Description = "Full system access - SaaS administrator with unrestricted permissions",
                IsSystemRole = true,
                IsTemplate = false,
                ClinicId = null,
                DisplayOrder = 1,
                ColorCode = "#DC2626", // Red
                IconClass = "fa-crown"
            },
            new()
            {
                Name = "SupportManager",
                Description = "Customer support - can view clinics and assist users with troubleshooting",
                IsSystemRole = true,
                IsTemplate = false,
                ClinicId = null,
                DisplayOrder = 2,
                ColorCode = "#2563EB", // Blue
                IconClass = "fa-headset"
            },
            new()
            {
                Name = "BillingManager",
                Description = "Billing and subscription management - handles payments and renewals",
                IsSystemRole = true,
                IsTemplate = false,
                ClinicId = null,
                DisplayOrder = 3,
                ColorCode = "#16A34A", // Green
                IconClass = "fa-dollar-sign"
            }
        };
    }

    private static List<Role> GetTemplateRoles()
    {
        return new List<Role>
        {
            new()
            {
                Name = "Clinic Admin",
                Description = "Full clinic management access - manages staff, patients, and settings",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 10,
                ColorCode = "#9333EA", // Purple
                IconClass = "fa-user-shield"
            },
            new()
            {
                Name = "Doctor",
                Description = "Medical practitioner - provides patient care and manages medical records",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 11,
                ColorCode = "#0891B2", // Cyan
                IconClass = "fa-user-md"
            },
            new()
            {
                Name = "Nurse",
                Description = "Nursing staff - assists with patient care and record viewing",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 12,
                ColorCode = "#DB2777", // Pink
                IconClass = "fa-user-nurse"
            },
            new()
            {
                Name = "Receptionist",
                Description = "Front desk operations - manages appointments and basic billing",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 13,
                ColorCode = "#EA580C", // Orange
                IconClass = "fa-desktop"
            }
        };
    }
}