using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class DbSeeder
{
    private readonly IEnumerable<ISingleSeeder> _seeders;
    private readonly NabdCareDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public DbSeeder(
        NabdCareDbContext dbContext,
        ITenantContext tenantContext,
        IEnumerable<ISingleSeeder> seeders
    )
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _seeders = seeders;
    }

    public async Task SeedAsync()
    {
        // Temporarily elevate tenant context for migrations
        var prevSuper = _tenantContext.IsSuperAdmin;
        _tenantContext.IsSuperAdmin = true;

        try
        {
            // Apply migrations first
            await _dbContext.Database.MigrateAsync();

            // Run all dedicated seeders
            foreach (var seeder in _seeders)
            {
                await seeder.SeedAsync();
            }
        }
        finally
        {
            _tenantContext.IsSuperAdmin = prevSuper;
        }
    }
}