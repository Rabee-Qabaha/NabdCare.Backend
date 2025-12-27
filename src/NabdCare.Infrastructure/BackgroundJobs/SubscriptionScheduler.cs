using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NabdCare.Infrastructure.BackgroundJobs;

public class SubscriptionScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionScheduler> _logger;

    public SubscriptionScheduler(
        IServiceProvider serviceProvider,
        ILogger<SubscriptionScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

// src/NabdCare.Infrastructure/BackgroundJobs/SubscriptionScheduler.cs

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⏳ SubscriptionScheduler service started.");

        // ✅ STEP 1: Run immediately on startup to catch up on missed jobs
        // (This ensures if you deploy at 00:05, you don't miss the midnight run)
        await RunJobAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1); // Next Midnight UTC
            var delay = nextRun - now;

            _logger.LogInformation("💤 Next scheduled run in {Time} at {NextRun} UTC", delay, nextRun);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunJobAsync(); // ✅ STEP 2: Run on schedule
            }
            catch (TaskCanceledException) { break; }
        }
    }

// Helper method to keep code DRY
    private async Task RunJobAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var job = scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleJob>();
                _logger.LogInformation("🚀 Executing SubscriptionLifecycleJob...");
                await job.ExecuteAsync();
                _logger.LogInformation("✅ SubscriptionLifecycleJob finished.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error executing subscription job.");
        }
    }
}