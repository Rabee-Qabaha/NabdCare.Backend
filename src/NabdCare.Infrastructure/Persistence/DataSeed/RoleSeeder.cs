using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Seeds default system roles used by NabdCare.
/// Works in harmony with RolePermissionsSeeder and PermissionsSeeder.
/// 
/// Roles:
/// 1. SuperAdmin - System-wide administrator (isSystemRole: true)
/// 2. SystemAdministrator - Platform operations manager (isTemplate: true, system-level)
/// 3. BillingManager - Subscription and payment manager (isTemplate: true, system-level)
/// 4. SupportManager - Customer support specialist (isTemplate: true, system-level)
/// 5. ClinicAdmin - Clinic-level administrator (isTemplate: true, clinic-level)
/// 6. Doctor - Healthcare provider (isTemplate: true, clinic-level)
/// 7. Receptionist - Front-desk staff (isTemplate: true, clinic-level)
/// 8. Nurse - Nursing staff (isTemplate: true, clinic-level)
/// 9. LabTechnician - Laboratory technician (isTemplate: true, clinic-level)
/// 
/// Author: Rabee Qabaha
/// Updated: 2025-11-09
/// </summary>
public class RoleSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _db;
    private readonly ILogger<RoleSeeder> _logger;

    public int Order => 1;

    public RoleSeeder(NabdCareDbContext db, ILogger<RoleSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding default system and clinic roles...");

        // Load existing roles (ignoring soft delete filters)
        var existingRoles = await _db.Roles
            .IgnoreQueryFilters()
            .ToListAsync();

        var rolesToSeed = new List<Role>
        {
            // ============================================
            // SYSTEM ROLES (isSystemRole: true)
            // ============================================
            new()
            {
                Name = "SuperAdmin",
                Description = "System-wide administrator with unrestricted access to all platform features.",
                IsSystemRole = true,
                IsTemplate = false,
                ClinicId = null,
                DisplayOrder = 1,
                IconClass = "pi-crown",
                ColorCode = "#DC2626", // Red
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },

            // ============================================
            // SYSTEM-LEVEL PRESET TEMPLATES (isTemplate: true, system-level, clinicId: null)
            // ============================================
            new()
            {
                Name = "SystemAdministrator",
                Description = "Platform administrator responsible for managing clinics and system operations.",
                IsSystemRole = true,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 2,
                IconClass = "pi-cog",
                ColorCode = "#3B82F6", // Blue
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "BillingManager",
                Description = "Manages clinic subscriptions, billing operations, and revenue tracking.",
                IsSystemRole = true,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 3,
                IconClass = "pi-credit-card",
                ColorCode = "#10B981", // Green
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "SupportManager",
                Description = "Customer support specialist with read-only access for troubleshooting and assistance.",
                IsSystemRole = true,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 4,
                IconClass = "pi-headphones",
                ColorCode = "#F59E0B", // Amber
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },

            // ============================================
            // CLINIC-LEVEL PRESET TEMPLATES (isTemplate: true, clinic-level, clinicId: null)
            // ============================================
            new()
            {
                Name = "ClinicAdmin",
                Description = "Clinic-level administrator responsible for managing users, appointments, and clinic billing.",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 5,
                IconClass = "pi-shield",
                ColorCode = "#8B5CF6", // Purple
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "Doctor",
                Description = "Healthcare provider with access to patient medical records and appointments.",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 6,
                IconClass = "pi-user-md",
                ColorCode = "#06B6D4", // Cyan
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "Receptionist",
                Description = "Front-desk staff managing patient registrations and appointment scheduling.",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 7,
                IconClass = "pi-phone",
                ColorCode = "#14B8A6", // Teal
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "Nurse",
                Description = "Nursing staff with access to patient care, medical records, and appointments.",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 8,
                IconClass = "pi-heart-fill",
                ColorCode = "#EC4899", // Pink
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "LabTechnician",
                Description = "Laboratory technician with access to patient data and laboratory test records.",
                IsSystemRole = false,
                IsTemplate = true,
                ClinicId = null,
                DisplayOrder = 9,
                IconClass = "pi-flask",
                ColorCode = "#A78BFA", // Violet
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            }
        };

        int addedCount = 0;

        foreach (var role in rolesToSeed)
        {
            if (!existingRoles.Any(r => r.Name == role.Name))
            {
                role.Id = Guid.NewGuid();
                _db.Roles.Add(role);
                addedCount++;
                _logger.LogInformation("   ➕ Added role: {RoleName} (DisplayOrder: {DisplayOrder})", role.Name, role.DisplayOrder);
            }
        }

        if (addedCount > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ {Count} new roles seeded successfully.", addedCount);
        }
        else
        {
            _logger.LogInformation("✅ All default roles already exist, skipping seeding.");
        }
    }
}