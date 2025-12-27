using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// 🌱 Dynamically seeds all permissions into the database by scanning
/// <see cref="Permissions"/> and its nested classes via reflection.
///
/// It automatically picks up new classes like 'Plans' and 'Branches' 
/// without needing manual code updates here.
///
/// Author: Rabee Qabaha
/// Updated: 2025-11-09
/// </summary>
public class PermissionsSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<PermissionsSeeder> _logger;

    public int Order => 2;

    public PermissionsSeeder(
        NabdCareDbContext dbContext,
        ILogger<PermissionsSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding permissions dynamically from Permissions constants...");

        var permissions = GetAllPermissionsWithDescriptions();
        var addedCount = 0;

        // Get existing permissions to avoid DB roundtrips in the loop
        var existingNames = await _dbContext.AppPermissions
            .IgnoreQueryFilters()
            .Select(p => p.Name)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingNames);

        foreach (var permission in permissions)
        {
            if (!existingSet.Contains(permission.Name))
            {
                permission.Id = Guid.NewGuid();
                permission.CreatedAt = DateTime.UtcNow;
                permission.CreatedBy = "System:Seeder";
                permission.IsDeleted = false;

                _dbContext.AppPermissions.Add(permission);
                addedCount++;

                _logger.LogDebug("   ➕ Added permission: {Name}", permission.Name);
            }
        }

        if (addedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ {Count} new permissions seeded successfully.", addedCount);
        }
        else
        {
            _logger.LogInformation("✅ All permissions already exist, skipping seeding.");
        }

        await WarnAboutOrphanedPermissionsAsync(permissions.Select(p => p.Name).ToList());
    }

    private static List<AppPermission> GetAllPermissionsWithDescriptions()
    {
        var list = new List<AppPermission>();
        var parentType = typeof(Permissions);

        // This picks up all nested classes (Clinics, Plans, Branches, etc.)
        foreach (var nested in parentType.GetNestedTypes(BindingFlags.Public))
        {
            var constants = nested
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToList();

            var descField = nested
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(f => f.FieldType == typeof(Dictionary<string, string>));

            Dictionary<string, string>? descriptions = null;
            if (descField != null)
            {
                descriptions = (Dictionary<string, string>?)descField.GetValue(null);
            }

            foreach (var name in constants)
            {
                var description = descriptions?.TryGetValue(name, out var desc) == true
                    ? desc
                    : $"Permission: {name}";

                list.Add(new AppPermission
                {
                    Name = name,
                    Description = description
                });
            }
        }

        return list;
    }

    private async Task WarnAboutOrphanedPermissionsAsync(List<string> codePermissions)
    {
        var dbPermissions = await _dbContext.AppPermissions
            .IgnoreQueryFilters()
            .Select(p => p.Name)
            .ToListAsync();

        var orphaned = dbPermissions
            .Where(name => !codePermissions.Contains(name))
            .OrderBy(n => n)
            .ToList();

        if (orphaned.Any())
        {
            _logger.LogWarning("⚠️ {Count} permissions exist in DB but not in code: {List}",
                orphaned.Count,
                string.Join(", ", orphaned));
        }
    }
}