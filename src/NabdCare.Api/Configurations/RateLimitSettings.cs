namespace NabdCare.Api.Configurations;

public class RateLimitSettings
{
    public RateLimitPolicySettings Auth { get; set; } = new();
    public RateLimitPolicySettings Api { get; set; } = new();
}

public class RateLimitPolicySettings
{
    /// <summary>
    /// Window size in minutes for fixed-window policies or replenishment period for token-bucket.
    /// </summary>
    public int WindowMinutes { get; set; } = 1;

    /// <summary>
    /// Number of permits allowed per window (fixed-window) or tokens per period (token-bucket).
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Optional queue limit for waiting requests (0 = no queue).
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}