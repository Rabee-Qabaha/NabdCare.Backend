using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class CreateClinicRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public decimal SubscriptionFee { get; set; }
    public int BranchCount { get; set; } = 1;
}