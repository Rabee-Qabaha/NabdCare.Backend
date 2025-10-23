// Infrastructure/BackgroundJobs/SubscriptionExpirationJob.cs
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Enums;

namespace NabdCare.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to automatically expire subscriptions
/// Run daily at 00:00 UTC
/// </summary>
public class SubscriptionExpirationJob
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ILogger<SubscriptionExpirationJob> _logger;

    public SubscriptionExpirationJob(
        IClinicRepository clinicRepository,
        ILogger<SubscriptionExpirationJob> logger)
    {
        _clinicRepository = clinicRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting subscription expiration check at {Time}", DateTime.UtcNow);

        var now = DateTime.UtcNow;
        var expiredClinics = await _clinicRepository.GetWithExpiredSubscriptionsAsync();
        
        var count = 0;
        foreach (var clinic in expiredClinics)
        {
            if (clinic.Status == SubscriptionStatus.Active)
            {
                clinic.Status = SubscriptionStatus.Expired;
                clinic.UpdatedAt = now;
                clinic.UpdatedBy = "System:ExpirationJob";

                var expiredSubscription = clinic.Subscriptions?
                    .Where(s => !s.IsDeleted && s.EndDate <= now)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefault();

                if (expiredSubscription != null)
                {
                    expiredSubscription.Status = SubscriptionStatus.Expired;
                    expiredSubscription.UpdatedAt = now;
                    expiredSubscription.UpdatedBy = "System:ExpirationJob";
                }

                await _clinicRepository.UpdateAsync(clinic);
                
                _logger.LogWarning("Clinic {ClinicId} ({ClinicName}) subscription expired automatically",
                    clinic.Id, clinic.Name);
                
                count++;
            }
        }

        _logger.LogInformation("Subscription expiration check completed. {Count} clinics expired", count);
    }
}