namespace NabdCare.Api.Configurations;

public class RateLimitSettings
{
    public AuthRateLimitSettings Auth { get; set; } = new();
}

public class AuthRateLimitSettings
{
    public int WindowMinutes { get; set; } = 1;
    public int PermitLimit { get; set; } = 5;
}