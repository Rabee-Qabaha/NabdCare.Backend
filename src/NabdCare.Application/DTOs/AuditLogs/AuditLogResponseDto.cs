namespace NabdCare.Application.DTOs.AuditLogs;

public class AuditLogResponseDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public Guid? ClinicId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = "";
    public string? Changes { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}