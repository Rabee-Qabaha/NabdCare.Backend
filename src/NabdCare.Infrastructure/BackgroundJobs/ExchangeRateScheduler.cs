using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NabdCare.Infrastructure.BackgroundJobs;

public class ExchangeRateScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeRateScheduler> _logger;

    public ExchangeRateScheduler(
        IServiceProvider serviceProvider,
        ILogger<ExchangeRateScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⏳ Exchange Rate Scheduler service started.");

        // Run immediately on startup
        await RunJobAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromHours(1);
            _logger.LogInformation("💤 Next exchange rate fetch in {Time}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await RunJobAsync();
            }
            catch (TaskCanceledException) 
            { 
                break; 
            }
        }
    }

    private async Task RunJobAsync()
    {
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var job = scope.ServiceProvider.GetRequiredService<ExchangeRateFetcherJob>();
                _logger.LogInformation("🚀 Executing ExchangeRateFetcherJob...");
                await job.ExecuteAsync();
                _logger.LogInformation("✅ ExchangeRateFetcherJob finished.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error executing ExchangeRateFetcherJob.");
        }
    }
}