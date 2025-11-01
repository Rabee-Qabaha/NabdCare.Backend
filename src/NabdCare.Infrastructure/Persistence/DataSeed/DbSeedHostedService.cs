using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Hosted service that triggers database seeding once at startup.
/// Ensures seeders run in the correct order and logs progress clearly.
/// 
/// Author: Rabee Qabaha
/// Updated: 2025-10-31
/// </summary>
public class DbSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbSeedHostedService> _logger;

    public DbSeedHostedService(IServiceProvider serviceProvider, ILogger<DbSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🧩 Starting {ServiceName}...", nameof(DbSeedHostedService));

        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();

        try
        {
            await seeder.SeedAsync();
            _logger.LogInformation("✅ Database seeding completed successfully on startup.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "❌ Database seeding failed at startup.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🧩 {ServiceName} stopped.", nameof(DbSeedHostedService));
        return Task.CompletedTask;
    }
}