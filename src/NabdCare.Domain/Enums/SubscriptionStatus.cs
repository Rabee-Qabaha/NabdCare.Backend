using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

/// <summary>
/// Subscription status enum for clinic subscriptions.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-22 20:53:22 UTC
/// </summary>
[ExportTsEnum]
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and clinic can operate normally
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Subscription is Inactive and clinic can't operate normally
    /// </summary>
    Inactive = 7,
    
    /// <summary>
    /// Subscription has expired (EndDate passed)
    /// </summary>
    Expired = 1,
    
    /// <summary>
    /// Subscription was cancelled by clinic or admin
    /// </summary>
    Cancelled = 2,
    
    /// <summary>
    /// Subscription is temporarily suspended (non-payment, violation, etc.)
    /// </summary>
    Suspended = 3,
    
    /// <summary>
    /// Trial period subscription
    /// </summary>
    Trial = 4,
    
    /// <summary>
    /// Future subscription
    /// </summary>
    Future = 5,
    PastDue = 6
}