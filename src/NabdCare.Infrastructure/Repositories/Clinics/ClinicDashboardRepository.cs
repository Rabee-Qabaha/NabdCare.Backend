using Microsoft.EntityFrameworkCore;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.Interfaces.Clinics;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using NabdCare.Domain.Enums.Invoice;
using NabdCare.Infrastructure.Persistence;
using System.Globalization;

namespace NabdCare.Infrastructure.Repositories.Clinics;

public class ClinicDashboardRepository : IClinicDashboardRepository
{
    private readonly NabdCareDbContext _db;

    public ClinicDashboardRepository(NabdCareDbContext db)
    {
        _db = db;
    }

    public async Task<ClinicDashboardStatsDto?> GetClinicStatsAsync(Guid clinicId)
    {
        // 1. Fetch Core Identity
        var clinic = await _db.Clinics
            .AsNoTracking()
            .Select(c => new 
            { 
                c.Id, c.Name, c.Slug, c.LogoUrl, c.Status, 
                c.CreatedAt, c.TaxNumber, c.RegistrationNumber, c.Settings, c.Email
            })
            .FirstOrDefaultAsync(c => c.Id == clinicId);

        if (clinic == null) return null;

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // 2. Run Queries Sequentially (Required for single DbContext)
        
        // --- USERS ---
        var currentUserCount = await _db.Users
            .CountAsync(u => u.ClinicId == clinicId && u.IsActive && !u.IsDeleted);

        var prevUserCount = await _db.Users
            .CountAsync(u => u.ClinicId == clinicId && u.IsActive && !u.IsDeleted && u.CreatedAt <= thirtyDaysAgo);

        // --- BRANCHES ---
        var branchCount = await _db.Branches
            .CountAsync(b => b.ClinicId == clinicId && b.IsActive && !b.IsDeleted);

        // --- PATIENTS ---
        var currentPatientCount = await _db.Patients
            .CountAsync(p => p.ClinicId == clinicId && !p.IsDeleted);

        var prevPatientCount = await _db.Patients
            .CountAsync(p => p.ClinicId == clinicId && !p.IsDeleted && p.CreatedAt <= thirtyDaysAgo);

        // --- INVOICES ---
        var hasOverdueInvoices = await _db.Invoices
            .AnyAsync(i => 
                i.ClinicId == clinicId && 
                i.SubscriptionId != null && 
                i.Status != InvoiceStatus.Paid && 
                i.Status != InvoiceStatus.Draft &&
                i.Status != InvoiceStatus.Void && 
                i.DueDate < DateTime.UtcNow
            );

        // --- ADMIN ---
        var admin = await _db.Users
            .AsNoTracking()
            .Where(u => u.ClinicId == clinicId && !u.IsDeleted)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new { u.Id, u.FullName })
            .FirstOrDefaultAsync();

        // --- SUBSCRIPTION ---
        var sub = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.ClinicId == clinicId)
            .OrderByDescending(s => s.EndDate)
            .Select(s => new { s.Status, s.EndDate, s.PlanId })
            .FirstOrDefaultAsync();

        // 3. Fetch Last Login (Audit) - Optional check
        DateTime? lastLogin = null;
        if (admin != null)
        {
            lastLogin = await _db.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == admin.Id && a.Action == "Login")
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Timestamp)
                .FirstOrDefaultAsync();
        }

        // 4. Map & Return
        return new ClinicDashboardStatsDto
        {
            ClinicId = clinic.Id,
            Name = clinic.Name,
            Identifier = clinic.Slug,
            LogoUrl = clinic.LogoUrl,
            IsActive = clinic.Status == SubscriptionStatus.Active,
            
            // Operational Metrics
            ActiveUsersCount = currentUserCount,
            TotalBranches = branchCount,
            ActivePatientsCount = currentPatientCount,

            // Calculated Growth Rates
            StaffGrowthRate = CalculateGrowth(currentUserCount, prevUserCount),
            PatientGrowthRate = CalculateGrowth(currentPatientCount, prevPatientCount),

            // Subscription
            SubscriptionPlan = FormatPlanName(sub?.PlanId),
            SubscriptionStatus = sub?.Status ?? SubscriptionStatus.Inactive,
            SubscriptionExpiresAt = sub?.EndDate,
            HasOverdueInvoices = hasOverdueInvoices,
            
            // Admin & System
            PrimaryAdminName = admin?.FullName ?? clinic.Email,
            LastLoginAt = lastLogin,
            CreatedAt = clinic.CreatedAt,
            TaxNumber = clinic.TaxNumber,
            RegistrationNumber = clinic.RegistrationNumber,
            Settings = clinic.Settings ?? new ClinicSettings()
        };
    }

    private static double CalculateGrowth(int current, int previous)
    {
        if (previous == 0) return current > 0 ? 100.0 : 0.0;
        return Math.Round(((double)(current - previous) / previous) * 100, 1);
    }

    private static string FormatPlanName(string? planId)
    {
        if (string.IsNullOrEmpty(planId)) return "Free / None";
        return CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(planId.Replace("_", " ").Replace("-", " "));
    }
}