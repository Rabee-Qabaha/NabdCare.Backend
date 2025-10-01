namespace NabdCare.Application.DTOs.Clinics;

public class ClinicResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public string SubscriptionType { get; set; } = string.Empty;
    public decimal SubscriptionFee { get; set; }
    public int BranchCount { get; set; } = 1;
}