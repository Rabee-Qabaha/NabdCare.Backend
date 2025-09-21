using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

public class DbSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DbSeedHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        seeder.Seed();
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}