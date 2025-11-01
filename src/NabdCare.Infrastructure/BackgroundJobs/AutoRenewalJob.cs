using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Clinics.Subscriptions;

namespace NabdCare.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job that automatically renews subscriptions
/// eligible for auto-renewal (within grace window).
/// Runs periodically (e.g., daily at midnight UTC).
/// </summary>
public class AutoRenewalJob
{
    private readonly ISubscriptionService _service;
    private readonly ILogger<AutoRenewalJob> _logger;

    public AutoRenewalJob(
        ISubscriptionService service,
        ILogger<AutoRenewalJob> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the auto-renewal job.
    /// </summary>
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("🔄 AutoRenewalJob started at {Time}", now);

        try
        {
            // Delegate full logic to the service layer
            var renewedCount = await _service.ProcessAutoRenewalsAsync(now);

            if (renewedCount == 0)
                _logger.LogInformation("✅ No subscriptions eligible for auto-renewal at this time.");
            else
                _logger.LogInformation("✅ AutoRenewalJob finished successfully. Renewed {Count} subscriptions.", renewedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AutoRenewalJob encountered an error at {Time}.", now);
        }
    }
}