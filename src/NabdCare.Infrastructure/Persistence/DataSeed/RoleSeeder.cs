using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Seeds default system roles used by NabdCare.
/// Works in harmony with RolePermissionsSeeder and PermissionsSeeder.
/// 
/// Author: Rabee Qabaha
/// Updated: 2025-10-31
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
        _logger.LogInformation("🌱 Seeding default system roles...");

        // Load existing roles (ignoring soft delete filters)
        var existingRoles = await _db.Roles
            .IgnoreQueryFilters()
            .ToListAsync();

        var rolesToSeed = new List<Role>
        {
            new()
            {
                Name = "SuperAdmin",
                Description = "System-wide administrator with unrestricted access to all features.",
                IsSystemRole = true,
                IsTemplate = false,
                ClinicId = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "ClinicAdmin",
                Description = "Clinic-level administrator responsible for managing users, appointments, and billing.",
                IsTemplate = true,
                ClinicId = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "Doctor",
                Description = "Healthcare provider with access to patient medical records and appointments.",
                IsTemplate = true,
                ClinicId = null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            },
            new()
            {
                Name = "Receptionist",
                Description = "Front-desk staff managing patient registrations and appointments.",
                IsTemplate = true,
                ClinicId = null,
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
                _logger.LogInformation("   ➕ Added role: {RoleName}", role.Name);
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