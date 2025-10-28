namespace NabdCare.Application.DTOs.AuditLogs;

public class AuditLogListRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? UserId { get; set; }

    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }

    public string? Search { get; set; }
}