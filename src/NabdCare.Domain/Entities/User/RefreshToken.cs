namespace NabdCare.Domain.Entities.User;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    // public User User { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
}