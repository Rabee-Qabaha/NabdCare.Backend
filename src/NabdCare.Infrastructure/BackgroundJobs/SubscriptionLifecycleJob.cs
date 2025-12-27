using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Subscriptions;

namespace NabdCare.Infrastructure.BackgroundJobs;

public class SubscriptionLifecycleJob
{
    private readonly ISubscriptionService _service;
    private readonly ILogger<SubscriptionLifecycleJob> _logger;

    public SubscriptionLifecycleJob(ISubscriptionService service, ILogger<SubscriptionLifecycleJob> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("🔄 SubscriptionLifecycleJob started at {Time}", now);

        try
        {
            var activated = await _service.ActivateFutureSubscriptionsAsync(now);
            var renewed = await _service.ProcessAutoRenewalsAsync(now);
            var cancelled = await _service.ProcessScheduledCancellationsAsync(now);
            var expired = await _service.ProcessExpirationsAsync(now);

            _logger.LogInformation("Report: {Activated} active, {Renewed} renewed, {Cancelled} cancelled, {Expired} expired.", 
                activated, renewed, cancelled, expired);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SubscriptionLifecycleJob failed.");
        }
    }
}