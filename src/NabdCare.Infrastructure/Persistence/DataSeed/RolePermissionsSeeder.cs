using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// 🌱 Seeds default role-permission mappings.
/// Updated to include Invoices, Plans, Branches, and Add-on permissions.
/// </summary>
public class RolePermissionsSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _db;
    private readonly ILogger<RolePermissionsSeeder> _logger;

    public int Order => 3;

    public RolePermissionsSeeder(NabdCareDbContext db, ILogger<RolePermissionsSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding role-permission mappings...");

        var roles = await _db.Roles.IgnoreQueryFilters().ToListAsync();
        if (!roles.Any())
        {
            _logger.LogWarning("⚠️ No roles found — run RoleSeeder first.");
            return;
        }

        var appPermissions = await _db.AppPermissions.IgnoreQueryFilters().ToListAsync();
        if (!appPermissions.Any())
        {
            _logger.LogWarning("⚠️ No AppPermissions found — run PermissionsSeeder first.");
            return;
        }

        // ===============================
        // 🔐 SUPER ADMIN (All Access)
        // ===============================
        var superAdminRole = roles.FirstOrDefault(r => r.Name == "SuperAdmin");
        if (superAdminRole != null)
        {
            await AssignAllPermissionsAsync(superAdminRole, appPermissions);
        }

        // ===============================
        // 👨‍💼 SYSTEM ADMINISTRATOR
        // ===============================
        var systemAdminRole = roles.FirstOrDefault(r => r.Name == "SystemAdministrator");
        if (systemAdminRole != null)
        {
            await AssignPermissionsAsync(systemAdminRole, appPermissions, new[]
            {
                // Clinics
                Permissions.Clinics.ViewAll,
                Permissions.Clinics.Create,
                Permissions.Clinics.Edit,
                Permissions.Clinics.Delete,
                Permissions.Clinics.ManageStatus,
                Permissions.Clinics.ViewStats,

                // Users & Roles
                Permissions.Users.ViewAll,
                Permissions.Users.Create,
                Permissions.Users.Edit,
                Permissions.Users.Delete,
                Permissions.Users.Activate,
                Permissions.Users.ChangeRole,
                Permissions.Users.ResetPassword,
                Permissions.Roles.ViewAll,
                Permissions.Roles.ViewSystem,
                Permissions.Roles.Create,
                Permissions.Roles.Edit,
                
                // Plans & Financials (System View)
                Permissions.Plans.View,
                Permissions.Plans.Manage,
                Permissions.Invoices.ViewAll,
                Permissions.Invoices.Void,
                Permissions.Invoices.Download,
                Permissions.Invoices.WriteOff, // ✅ NEW: Bad Debt

                Permissions.Payments.ViewAll, // ✅ NEW: View all payments
                Permissions.Payments.Refund,
                Permissions.Payments.ManageCheques, // ✅ NEW: Manage Cheques

                Permissions.AppPermissions.View,
                Permissions.Reports.ViewDashboard,
                Permissions.AuditLogs.View
            });
        }

        // ===============================
        // 💰 BILLING MANAGER (System Level)
        // ===============================
        var billingManagerRole = roles.FirstOrDefault(r => r.Name == "BillingManager");
        if (billingManagerRole != null)
        {
            await AssignPermissionsAsync(billingManagerRole, appPermissions, new[]
            {
                // Subscriptions
                Permissions.Subscriptions.ViewAll,
                Permissions.Subscriptions.Create,
                Permissions.Subscriptions.Edit,
                Permissions.Subscriptions.Renew,
                Permissions.Subscriptions.PurchaseAddons,
                
                // Plans
                Permissions.Plans.View,

                // Clinics
                Permissions.Clinics.ViewAll,
                
                // Financials
                Permissions.Payments.ViewAll, // ✅ NEW
                Permissions.Payments.Process,
                Permissions.Payments.Refund,
                Permissions.Payments.ViewReports,
                Permissions.Payments.ManageCheques, // ✅ NEW
                Permissions.Payments.Cancel, // ✅ NEW
                
                // Invoices
                Permissions.Invoices.ViewAll,
                Permissions.Invoices.Pay,
                Permissions.Invoices.Download,
                Permissions.Invoices.Void,
                Permissions.Invoices.WriteOff, // ✅ NEW
                
                Permissions.Reports.ViewFinancialReports
            });
        }

        // ===============================
        // 🏥 CLINIC ADMIN (Customer)
        // ===============================
        var clinicAdminRole = roles.FirstOrDefault(r => r.Name == "ClinicAdmin");
        if (clinicAdminRole != null)
        {
            await AssignPermissionsAsync(clinicAdminRole, appPermissions, new[]
            {
                // Clinic Management
                Permissions.Clinic.View,
                Permissions.Clinic.Edit,

                // 🌳 BRANCHES
                Permissions.Branches.View,
                Permissions.Branches.Create, 
                Permissions.Branches.Edit,
                Permissions.Branches.Delete,
                Permissions.Branches.ToggleStatus, 
                Permissions.Branches.SetMain,      

                // Subscriptions & Billing
                Permissions.Subscriptions.View,
                Permissions.Subscriptions.Renew,
                Permissions.Subscriptions.PurchaseAddons, 
                Permissions.Plans.View, 
                
                // Invoices (Own Clinic)
                Permissions.Invoices.View,
                Permissions.Invoices.Pay,
                Permissions.Invoices.Download,

                // Users
                Permissions.Users.View,
                Permissions.Users.Create, 
                Permissions.Users.Edit,
                Permissions.Users.Delete,
                Permissions.Users.ChangeRole,
                
                // Roles
                Permissions.Roles.ViewClinic,
                Permissions.Roles.Create,
                Permissions.Roles.Edit,
                Permissions.Roles.Delete,

                // Patients & Appts
                Permissions.Patients.View,
                Permissions.Patients.Create,
                Permissions.Appointments.View,
                Permissions.Appointments.Create,
                
                // Payments (Own Clinic)
                Permissions.Payments.View,
                Permissions.Payments.Create,
                Permissions.Payments.Allocate, // ✅ NEW
                Permissions.Payments.Refund,   // ✅ NEW (Self-Refund?)
                Permissions.Payments.Cancel,   // ✅ NEW
                Permissions.Payments.ViewReceipts, // ✅ NEW
                
                Permissions.Reports.ViewDashboard,
                Permissions.Reports.ViewFinancialReports // ✅ NEW
            });
        }

        // ===============================
        // 👨‍⚕️ DOCTOR
        // ===============================
        var doctorRole = roles.FirstOrDefault(r => r.Name == "Doctor");
        if (doctorRole != null)
        {
            await AssignPermissionsAsync(doctorRole, appPermissions, new[]
            {
                Permissions.Patients.View,
                Permissions.Patients.ViewDetails,
                Permissions.Patients.ViewMedicalHistory,
                Permissions.MedicalRecords.View,
                Permissions.MedicalRecords.Create,
                Permissions.MedicalRecords.Edit,
                Permissions.MedicalRecords.Prescribe,
                Permissions.Appointments.View,
                Permissions.Appointments.CheckIn
            });
        }

        // ===============================
        // 📞 RECEPTIONIST
        // ===============================
        var receptionistRole = roles.FirstOrDefault(r => r.Name == "Receptionist");
        if (receptionistRole != null)
        {
            await AssignPermissionsAsync(receptionistRole, appPermissions, new[]
            {
                Permissions.Appointments.View,
                Permissions.Appointments.Create,
                Permissions.Appointments.Edit,
                Permissions.Appointments.Cancel,
                Permissions.Appointments.ViewCalendar,
                Permissions.Patients.View,
                Permissions.Patients.Create,
                Permissions.Patients.Edit,
                
                // Payments
                Permissions.Payments.View,
                Permissions.Payments.Create,
                Permissions.Payments.Process,
                Permissions.Payments.ViewReceipts, // ✅ NEW
                Permissions.Invoices.View,
                Permissions.Invoices.Download
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("✅ Role-permission seeding completed successfully.");
    }

    private async Task AssignAllPermissionsAsync(Role role, List<AppPermission> allPermissions)
    {
        var existing = await _db.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var missing = allPermissions
            .Where(p => !existing.Contains(p.Id))
            .Select(p => new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = p.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            })
            .ToList();

        if (missing.Any())
        {
            _db.RolePermissions.AddRange(missing);
            _logger.LogInformation("🧩 Assigned {Count} permissions to {Role}.", missing.Count, role.Name);
        }
    }

    private async Task AssignPermissionsAsync(Role role, List<AppPermission> allPermissions, string[] permissionNames)
    {
        var existing = await _db.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.AppPermission!.Name)
            .ToListAsync();

        var toAssign = permissionNames
            .Where(p => !existing.Contains(p))
            .Select(name => allPermissions.FirstOrDefault(ap => ap.Name == name))
            .Where(ap => ap != null)
            .Select(ap => new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = ap!.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System:Seeder"
            })
            .ToList();

        if (toAssign.Any())
        {
            _db.RolePermissions.AddRange(toAssign);
            _logger.LogInformation("🧩 Assigned {Count} permissions to {Role}", toAssign.Count, role.Name);
        }
    }
}