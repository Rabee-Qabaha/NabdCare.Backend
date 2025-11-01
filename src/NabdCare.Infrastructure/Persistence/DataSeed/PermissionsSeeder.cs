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
/// Supports per-permission descriptions through each nested class’s
/// <c>Descriptions</c> dictionary (optional).
///
/// Author: Rabee Qabaha
/// Updated: 2025-10-31
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

        foreach (var permission in permissions)
        {
            bool exists = await _dbContext.AppPermissions
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Name == permission.Name);

            if (!exists)
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

        // Optional: Warn if some DB permissions are not in code anymore
        await WarnAboutOrphanedPermissionsAsync(permissions.Select(p => p.Name).ToList());
    }

    /// <summary>
    /// Extracts all permission constants and descriptions from <see cref="Permissions"/>.
    /// Each nested static class may optionally define:
    /// <code>
    /// public static readonly Dictionary&lt;string, string&gt; Descriptions = new()
    /// {
    ///     { View, "View a specific resource" },
    ///     { Edit, "Modify an existing resource" }
    /// };
    /// </code>
    /// </summary>
    private static List<AppPermission> GetAllPermissionsWithDescriptions()
    {
        var list = new List<AppPermission>();
        var parentType = typeof(Permissions);

        foreach (var nested in parentType.GetNestedTypes(BindingFlags.Public))
        {
            // collect constant string fields
            var constants = nested
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToList();

            // attempt to locate a Descriptions dictionary
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

    /// <summary>
    /// Logs any database permissions that are not defined in the code constants.
    /// Helps detect legacy or orphaned permissions.
    /// </summary>
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
        else
        {
            _logger.LogInformation("🟢 No orphaned permissions found; DB is in sync with code.");
        }
    }
}