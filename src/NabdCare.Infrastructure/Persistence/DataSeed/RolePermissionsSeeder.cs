using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// 🌱 Seeds default role-permission mappings for the system.
/// Automatically links roles with permissions defined in <see cref="Permissions"/>.
/// 
/// Roles Covered:
/// - SuperAdmin (all permissions)
/// - SystemAdministrator (platform management)
/// - BillingManager (subscription & billing)
/// - SupportManager (read-only support access)
/// - ClinicAdmin (clinic-level management)
/// - Doctor (clinical access)
/// - Receptionist (appointment & patient management)
/// - Nurse (patient care & medical records)
/// - LabTechnician (laboratory test records)
/// 
/// Works dynamically with seeded AppPermissions, ensuring consistency
/// even when new permissions are added later.
/// 
/// Author: Rabee Qabaha
/// Updated: 2025-11-09
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

        var roles = await _db.Roles
            .IgnoreQueryFilters()
            .ToListAsync();

        if (!roles.Any())
        {
            _logger.LogWarning("⚠️ No roles found — please run RoleSeeder first.");
            return;
        }

        var appPermissions = await _db.AppPermissions
            .IgnoreQueryFilters()
            .ToListAsync();

        if (!appPermissions.Any())
        {
            _logger.LogWarning("⚠️ No AppPermissions found — please run PermissionsSeeder first.");
            return;
        }

        // ===============================
        // 🔐 SUPER ADMIN — Full Access
        // ===============================
        var superAdminRole = roles.FirstOrDefault(r => r.Name == "SuperAdmin");
        if (superAdminRole != null)
        {
            _logger.LogInformation("   🔐 Configuring SuperAdmin...");
            await AssignAllPermissionsAsync(superAdminRole, appPermissions);
        }

        // ===============================
        // 👨‍💼 SYSTEM ADMINISTRATOR — Platform Ops
        // ===============================
        var systemAdminRole = roles.FirstOrDefault(r => r.Name == "SystemAdministrator");
        if (systemAdminRole != null)
        {
            _logger.LogInformation("   👨‍💼 Configuring SystemAdministrator...");
            await AssignPermissionsAsync(systemAdminRole, appPermissions, new[]
            {
                // Clinics
                Permissions.Clinics.ViewAll,
                Permissions.Clinics.Create,
                Permissions.Clinics.Edit,
                Permissions.Clinics.Delete,
                Permissions.Clinics.ManageStatus,
                Permissions.Clinics.ViewStats,

                // Users
                Permissions.Users.ViewAll,
                Permissions.Users.Create,
                Permissions.Users.Edit,
                Permissions.Users.Delete,
                Permissions.Users.Activate,
                Permissions.Users.ChangeRole,
                Permissions.Users.ResetPassword,

                // Roles & Permissions
                Permissions.Roles.ViewAll,
                Permissions.Roles.ViewSystem,
                Permissions.Roles.ViewTemplates,
                Permissions.AppPermissions.View,
                Permissions.AppPermissions.Assign, 
                Permissions.AppPermissions.Revoke,

                // Reports & Audit
                Permissions.Reports.ViewDashboard,
                Permissions.AuditLogs.View
            });
        }

        // ===============================
        // 💰 BILLING MANAGER — Subscriptions & Revenue
        // ===============================
        var billingManagerRole = roles.FirstOrDefault(r => r.Name == "BillingManager");
        if (billingManagerRole != null)
        {
            _logger.LogInformation("   💰 Configuring BillingManager...");
            await AssignPermissionsAsync(billingManagerRole, appPermissions, new[]
            {
                // Subscriptions (Full management)
                Permissions.Subscriptions.ViewAll,
                Permissions.Subscriptions.Create,
                Permissions.Subscriptions.Edit,
                Permissions.Subscriptions.Delete,
                Permissions.Subscriptions.ChangeStatus,
                Permissions.Subscriptions.Renew,
                Permissions.Subscriptions.ToggleAutoRenew,
                Permissions.Subscriptions.ViewActive,

                // Clinics (Read-only)
                Permissions.Clinics.ViewAll,
                Permissions.Clinics.ViewStats,

                // Payments & Invoices
                Permissions.Payments.View,
                Permissions.Payments.ViewReports,
                Permissions.Invoices.View,
                Permissions.Invoices.ViewReports,

                // Reports
                Permissions.Reports.ViewDashboard,
                Permissions.Reports.ViewFinancialReports
            });
        }

        // ===============================
        // 📞 SUPPORT MANAGER — Read-only Support Access
        // ===============================
        var supportManagerRole = roles.FirstOrDefault(r => r.Name == "SupportManager");
        if (supportManagerRole != null)
        {
            _logger.LogInformation("   📞 Configuring SupportManager...");
            await AssignPermissionsAsync(supportManagerRole, appPermissions, new[]
            {
                // Clinics (Read-only)
                Permissions.Clinics.ViewAll,
                Permissions.Clinics.Search,

                // Users (Read-only)
                Permissions.Users.ViewAll,
                Permissions.Users.ViewDetails,

                // Subscriptions (Read-only)
                Permissions.Subscriptions.ViewAll,

                // Audit & Logs (Read-only)
                Permissions.AuditLogs.View,

                // Reports (Read-only)
                Permissions.Reports.ViewDashboard
            });
        }

        // ===============================
        // 🏥 CLINIC ADMIN — Clinic-level Management
        // ===============================
        var clinicAdminRole = roles.FirstOrDefault(r => r.Name == "ClinicAdmin");
        if (clinicAdminRole != null)
        {
            _logger.LogInformation("   🏥 Configuring ClinicAdmin...");
            await AssignPermissionsAsync(clinicAdminRole, appPermissions, new[]
            {
                // Clinic
                Permissions.Clinic.View,
                Permissions.Clinic.Edit,
                Permissions.Clinic.ViewSettings,
                Permissions.Clinic.EditSettings,
                Permissions.Clinic.ManageBranches,

                // Subscriptions
                Permissions.Subscriptions.View,
                Permissions.Subscriptions.Renew,

                // Users
                Permissions.Users.View,
                Permissions.Users.ViewDetails,
                Permissions.Users.Create,
                Permissions.Users.Edit,
                Permissions.Users.Delete,
                Permissions.Users.ChangeRole,
                Permissions.Users.ResetPassword,

                // Patients
                Permissions.Patients.View,
                Permissions.Patients.ViewDetails,
                Permissions.Patients.Create,
                Permissions.Patients.Edit,
                Permissions.Patients.ViewMedicalHistory,
                Permissions.Patients.EditMedicalHistory,

                // Appointments
                Permissions.Appointments.View,
                Permissions.Appointments.Create,
                Permissions.Appointments.Edit,
                Permissions.Appointments.Cancel,

                // Payments
                Permissions.Payments.View,
                Permissions.Payments.Create,
                Permissions.Payments.ViewReports,

                // Reports
                Permissions.Reports.ViewDashboard,
                Permissions.Reports.ViewPatientReports,
                Permissions.Reports.Export,
                
                // Permission Management (For their staff)
                Permissions.AppPermissions.View,                
                Permissions.AppPermissions.ViewUserPermissions,
                Permissions.AppPermissions.Assign,              
                Permissions.AppPermissions.Revoke,
            });
        }

        // ===============================
        // 👨‍⚕️ DOCTOR — Patient & Appointment Focus
        // ===============================
        var doctorRole = roles.FirstOrDefault(r => r.Name == "Doctor");
        if (doctorRole != null)
        {
            _logger.LogInformation("   👨‍⚕️ Configuring Doctor...");
            await AssignPermissionsAsync(doctorRole, appPermissions, new[]
            {
                // Patients
                Permissions.Patients.View,
                Permissions.Patients.ViewDetails,
                Permissions.Patients.ViewMedicalHistory,

                // Medical Records
                Permissions.MedicalRecords.View,
                Permissions.MedicalRecords.Create,
                Permissions.MedicalRecords.Edit,
                Permissions.MedicalRecords.Prescribe,
                Permissions.MedicalRecords.ViewPrescriptions,

                // Appointments
                Permissions.Appointments.View,
                Permissions.Appointments.CheckIn
            });
        }

        // ===============================
        // 📞 RECEPTIONIST — Appointments + Patients
        // ===============================
        var receptionistRole = roles.FirstOrDefault(r => r.Name == "Receptionist");
        if (receptionistRole != null)
        {
            _logger.LogInformation("   📞 Configuring Receptionist...");
            await AssignPermissionsAsync(receptionistRole, appPermissions, new[]
            {
                // Appointments
                Permissions.Appointments.View,
                Permissions.Appointments.Create,
                Permissions.Appointments.Edit,
                Permissions.Appointments.Cancel,
                Permissions.Appointments.ViewCalendar,

                // Patients
                Permissions.Patients.View,
                Permissions.Patients.ViewDetails,
                Permissions.Patients.Create,
                Permissions.Patients.Edit
            });
        }

        // ===============================
        // 🧑‍⚕️ NURSE — Patient Care & Medical Records
        // ===============================
        var nurseRole = roles.FirstOrDefault(r => r.Name == "Nurse");
        if (nurseRole != null)
        {
            _logger.LogInformation("   🧑‍⚕️ Configuring Nurse...");
            await AssignPermissionsAsync(nurseRole, appPermissions, new[]
            {
                // Patients (Full access)
                Permissions.Patients.View,
                Permissions.Patients.ViewDetails,
                Permissions.Patients.Edit,
                Permissions.Patients.ViewMedicalHistory,
                Permissions.Patients.EditMedicalHistory,

                // Medical Records (Can create/edit but not prescribe)
                Permissions.MedicalRecords.View,
                Permissions.MedicalRecords.Create,
                Permissions.MedicalRecords.Edit,
                Permissions.MedicalRecords.ViewPrescriptions,

                // Appointments
                Permissions.Appointments.View,
                Permissions.Appointments.Edit
            });
        }

        // ===============================
        // 🧪 LAB TECHNICIAN — Laboratory Test Records
        // ===============================
        var labTechnicianRole = roles.FirstOrDefault(r => r.Name == "LabTechnician");
        if (labTechnicianRole != null)
        {
            _logger.LogInformation("   🧪 Configuring LabTechnician...");
            await AssignPermissionsAsync(labTechnicianRole, appPermissions, new[]
            {
                // Patients (Read-only)
                Permissions.Patients.View,
                Permissions.Patients.ViewDetails,

                // Medical Records (Create lab results, edit their own)
                Permissions.MedicalRecords.View,
                Permissions.MedicalRecords.Create,
                Permissions.MedicalRecords.Edit,

                // Appointments (View only)
                Permissions.Appointments.View
            });
        }

        // ===============================
        // 🔹 SAVE CHANGES
        // ===============================
        await _db.SaveChangesAsync();

        // ===============================
        // 🧠 Post-check: Unassigned permissions
        // ===============================
        var assignedPermissionIds = await _db.RolePermissions
            .IgnoreQueryFilters()
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync();

        var unassigned = appPermissions
            .Where(p => !assignedPermissionIds.Contains(p.Id))
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToList();

        if (unassigned.Any())
        {
            _logger.LogWarning("⚠️ {Count} permissions are NOT assigned to any role: {List}",
                unassigned.Count,
                string.Join(", ", unassigned));
        }
        else
        {
            _logger.LogInformation("🟢 All permissions are assigned to at least one role.");
        }

        _logger.LogInformation("✅ Role-permission seeding completed successfully.");
    }

    // =====================================================
    // 🔹 Assign All Permissions (SuperAdmin)
    // =====================================================
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
        else
        {
            _logger.LogInformation("🟢 {Role} already has all permissions.", role.Name);
        }
    }

    // =====================================================
    // 🔹 Assign Specific Permissions (Other Roles)
    // =====================================================
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
        else
        {
            _logger.LogInformation("🟢 {Role} already up-to-date", role.Name);
        }
    }
}