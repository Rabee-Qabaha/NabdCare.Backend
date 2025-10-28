namespace NabdCare.Domain.Entities.Permissions;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;

    // NEW: tenant scoping
    public Guid? ClinicId { get; set; }   // null for SuperAdmin (SaaS scope)

    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }

    public string? IssuedForUserAgent { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }
}