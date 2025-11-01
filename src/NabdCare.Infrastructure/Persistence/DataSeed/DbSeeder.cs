using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Orchestrates all data seeders in correct order:
/// 1️⃣ Roles → 2️⃣ Permissions → 3️⃣ RolePermissions → 4️⃣ SuperAdmin (optional)
/// 
/// Automatically migrates the database before running.
/// Author: Rabee Qabaha
/// Updated: 2025-10-31
/// </summary>
public class DbSeeder
{
    private readonly NabdCareDbContext _db;
    private readonly IEnumerable<ISingleSeeder> _seeders;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        NabdCareDbContext db,
        IEnumerable<ISingleSeeder> seeders,
        ILogger<DbSeeder> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _seeders = seeders ?? throw new ArgumentNullException(nameof(seeders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🚀 Starting database seeding...");

        try
        {
            // ====================================================
            // 1️⃣ Apply database migrations before seeding
            // ====================================================
            _logger.LogInformation("🧱 Applying migrations (if needed)...");
            await _db.Database.MigrateAsync();
            _logger.LogInformation("✅ Migrations applied successfully.");

            // ====================================================
            // 2️⃣ Run all registered seeders in order
            // ====================================================
            var orderedSeeders = _seeders.OrderBy(s => s.Order).ToList();

            _logger.LogInformation("📋 Running {Count} seeders in order...", orderedSeeders.Count);

            foreach (var seeder in orderedSeeders)
            {
                var seederName = seeder.GetType().Name;
                _logger.LogInformation("▶️ Running seeder: {Seeder} (Order: {Order})", seederName, seeder.Order);

                try
                {
                    await seeder.SeedAsync();
                    _logger.LogInformation("✅ {Seeder} completed successfully.", seederName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error running seeder {Seeder}", seederName);
                    throw; // stop the process if one fails
                }
            }

            _logger.LogInformation("🎉 All seeders executed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "💥 Database seeding failed: {Message}", ex.Message);
            throw;
        }
    }
}