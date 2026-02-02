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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⏳ Scheduler service started.");

        // ✅ STEP 1: Run immediately on startup to catch up on missed jobs
        await RunJobsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1); // Next Midnight UTC
            var delay = nextRun - now;

            _logger.LogInformation("💤 Next scheduled run in {Time} at {NextRun} UTC", delay, nextRun);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunJobsAsync(); // ✅ STEP 2: Run on schedule
            }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunJobsAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                // 1. Subscription Lifecycle
                var subJob = scope.ServiceProvider.GetRequiredService<SubscriptionLifecycleJob>();
                _logger.LogInformation("🚀 Executing SubscriptionLifecycleJob...");
                await subJob.ExecuteAsync();
                
                // 2. Invoice Overdue Check (New)
                var invoiceJob = scope.ServiceProvider.GetRequiredService<InvoiceOverdueJob>();
                _logger.LogInformation("🚀 Executing InvoiceOverdueJob...");
                await invoiceJob.RunAsync();
                
                _logger.LogInformation("✅ All scheduled jobs finished.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error executing scheduled jobs.");
        }
    }
}