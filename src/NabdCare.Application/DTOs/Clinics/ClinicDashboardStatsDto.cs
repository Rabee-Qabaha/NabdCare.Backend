using System;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

[ExportTsClass]
public class ClinicDashboardStatsDto
{
    // Identity
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }

    // Operational Counters
    public int ActiveUsersCount { get; set; }
    public int TotalBranches { get; set; }
    public int ActivePatientsCount { get; set; }

    // Subscription & Financial
    public string SubscriptionPlan { get; set; } = "None";
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool HasOverdueInvoices { get; set; }
    
    // Audit
    public DateTime? LastLoginAt { get; set; }
    public string? PrimaryAdminName { get; set; }
    
    public DateTime CreatedAt { get; set; } 
    public string? TaxNumber { get; set; }
    public string? RegistrationNumber { get; set; }
    public ClinicSettings Settings { get; set; } = new();
    
    public double StaffGrowthRate { get; set; }
    public double PatientGrowthRate { get; set; }
}