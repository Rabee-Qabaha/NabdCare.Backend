using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ClinicStatisticsDto
{
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public int BranchCount { get; set; }
    
    // Subscription stats
    public int TotalSubscriptions { get; set; }
    public SubscriptionResponseDto? CurrentSubscription { get; set; } // ✅ Using your DTO
    public bool IsSubscriptionActive { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsExpiringSoon { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
}