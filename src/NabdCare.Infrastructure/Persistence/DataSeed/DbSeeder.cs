using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class DbSeeder
{
    private readonly IEnumerable<ISingleSeeder> _seeders;
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        NabdCareDbContext dbContext,
        IEnumerable<ISingleSeeder> seeders,
        ILogger<DbSeeder> logger)
    {
        _dbContext = dbContext;
        _seeders = seeders;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🚀 Starting database seeding...");

        // Apply migrations first
        await _dbContext.Database.MigrateAsync();
        _logger.LogInformation("✅ Migrations applied.");

        // Run all seeders in order
        var orderedSeeders = _seeders.OrderBy(s => s.Order).ToList();
        
        _logger.LogInformation("📋 Running {Count} seeders...", orderedSeeders.Count);

        foreach (var seeder in orderedSeeders)
        {
            var seederName = seeder.GetType().Name;
            _logger.LogInformation("▶️  Running {Seeder} (Order: {Order})...", 
                seederName, seeder.Order);

            try
            {
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error running seeder {Seeder}", seederName);
                throw;
            }
        }

        _logger.LogInformation("🎉 Database seeding completed successfully!");
    }
}