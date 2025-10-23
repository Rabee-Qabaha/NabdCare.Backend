using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Seeds role-permission associations into the database.
/// Maps permissions to system and template roles.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 18:46:03 UTC
/// </summary>
public class RolePermissionsSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<RolePermissionsSeeder> _logger;

    public int Order => 3;

    public RolePermissionsSeeder(
        NabdCareDbContext dbContext,
        ILogger<RolePermissionsSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding role permissions...");

        var roles = await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => r.ClinicId == null)
            .ToListAsync();

        var permissions = await _dbContext.AppPermissions
            .IgnoreQueryFilters()
            .ToListAsync();

        if (!roles.Any())
        {
            _logger.LogWarning("⚠️  No roles found. Ensure RolesSeeder runs before this");
            return;
        }

        if (!permissions.Any())
        {
            _logger.LogWarning("⚠️  No permissions found. Ensure PermissionsSeeder runs before this");
            return;
        }

        var rolePermissionMappings = GetRolePermissionMappings();
        var assignedCount = 0;

        foreach (var mapping in rolePermissionMappings)
        {
            var roleName = mapping.Key;
            var permissionNames = mapping.Value;

            var role = roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
            {
                _logger.LogWarning("   ⚠️  Role '{RoleName}' not found, skipping", roleName);
                continue;
            }

            _logger.LogDebug("   📋 Processing role: {RoleName} ({Count} permissions)", 
                roleName, permissionNames.Length);

            foreach (var permissionName in permissionNames)
            {
                var permission = permissions.FirstOrDefault(p => p.Name == permissionName);
                if (permission == null)
                {
                    _logger.LogWarning("      ⚠️  Permission '{PermissionName}' not found", permissionName);
                    continue;
                }

                var exists = await _dbContext.RolePermissions
                    .IgnoreQueryFilters()
                    .AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);

                if (!exists)
                {
                    var rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System:Seeder",
                        IsDeleted = false
                    };

                    _dbContext.RolePermissions.Add(rolePermission);
                    assignedCount++;
                }
            }

            _logger.LogInformation("   ✅ Assigned {Count} permissions to {RoleName}", 
                permissionNames.Length, roleName);
        }

        if (assignedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ {Count} role-permission assignments created", assignedCount);
        }
        else
        {
            _logger.LogInformation("✅ All role permissions already assigned, skipping seed");
        }
    }

    private static Dictionary<string, string[]> GetRolePermissionMappings()
    {
        return new Dictionary<string, string[]>
        {
            // ============================================
            // 👑 SUPERADMIN - Full System Access
            // ============================================
            ["SuperAdmin"] = new[]
            {
                // ALL Clinic Management
                "Clinics.ViewAll",
                "Clinics.Create",
                "Clinics.Edit",
                "Clinics.Delete",
                "Clinics.HardDelete",
                "Clinics.ManageStatus",
                "Clinics.ViewStats",
                
                // ALL Subscription Management
                "Subscriptions.ViewAll",
                "Subscriptions.View",
                "Subscriptions.Create",
                "Subscriptions.Edit",
                "Subscriptions.Cancel",
                "Subscriptions.Renew",
                
                // ALL User Management
                "Users.ViewAll",
                "Users.View",
                "Users.ViewDetails",
                "Users.Create",
                "Users.Edit",
                "Users.Delete",
                "Users.Activate",
                "Users.ChangeRole",
                "Users.ResetPassword",
                "Users.ManagePermissions",
                
                // ALL Role Management
                "Roles.ViewAll",
                "Roles.View",
                "Roles.Create",
                "Roles.Edit",
                "Roles.Delete",
                "Roles.ManagePermissions",
                
                // ALL Permission Management
                "Permissions.View",
                "Permissions.Assign",
                "Permissions.Revoke",
                
                // ALL Patient Management
                "Patients.View",
                "Patients.ViewDetails",
                "Patients.Create",
                "Patients.Edit",
                "Patients.Delete",
                "Patients.ViewMedicalHistory",
                "Patients.EditMedicalHistory",
                "Patients.ViewDocuments",
                "Patients.UploadDocuments",
                "Patients.Export",
                
                // ALL Appointments
                "Appointments.View",
                "Appointments.Create",
                "Appointments.Edit",
                "Appointments.Cancel",
                "Appointments.CheckIn",
                "Appointments.ViewCalendar",
                
                // ALL Medical Records
                "MedicalRecords.View",
                "MedicalRecords.Create",
                "MedicalRecords.Edit",
                "MedicalRecords.Delete",
                "MedicalRecords.Prescribe",
                "MedicalRecords.ViewPrescriptions",
                
                // ALL Billing
                "Payments.View",
                "Payments.Create",
                "Payments.Edit",
                "Payments.Delete",
                "Payments.Process",
                "Payments.Refund",
                "Payments.ViewReports",
                "Invoices.View",
                "Invoices.Create",
                "Invoices.Edit",
                "Invoices.Send",
                "Invoices.ViewReports",
                
                // ALL Reports
                "Reports.ViewDashboard",
                "Reports.ViewPatientReports",
                "Reports.ViewFinancialReports",
                "Reports.ViewAppointmentReports",
                "Reports.Export",
                "Reports.Generate",
                
                // ALL Settings
                "Settings.View",
                "Settings.Edit",
                
                // ALL Audit Logs
                "AuditLogs.View",
                "AuditLogs.Export",
                
                // System Management
                "System.ManageSettings",
                "System.ViewLogs",
                "System.ManageRoles"
            },

            // ============================================
            // 🛠️ SUPPORT MANAGER - Customer Support
            // ============================================
            ["SupportManager"] = new[]
            {
                "Clinics.ViewAll",
                "Users.ViewAll",
                "Users.View",
                "Users.ViewDetails",
                "Users.ResetPassword",
                "Patients.View",
                "Patients.ViewDetails",
                "AuditLogs.View"
            },

            // ============================================
            // 💰 BILLING MANAGER - Subscription Management
            // ============================================
            ["BillingManager"] = new[]
            {
                "Clinics.ViewAll",
                "Clinics.Edit",
                "Subscriptions.ViewAll",
                "Subscriptions.Create",
                "Subscriptions.Edit",
                "Subscriptions.Cancel",
                "Subscriptions.Renew",
                "Payments.View",
                "Payments.ViewReports",
                "Invoices.View",
                "Invoices.ViewReports",
                "Reports.ViewFinancialReports"
            },

            // ============================================
            // 🏥 CLINIC ADMIN - Full Clinic Management
            // ============================================
            ["Clinic Admin"] = new[]
            {
                // Own Clinic Management
                "Clinic.View",
                "Clinic.Edit",
                "Clinic.ViewSettings",
                "Clinic.EditSettings",
                "Clinic.ManageBranches",
                
                // Own Subscription
                "Subscriptions.View",
                "Subscriptions.Renew",
                
                // User Management (within clinic)
                "Users.View",
                "Users.ViewDetails",
                "Users.Create",
                "Users.Edit",
                "Users.Delete",
                "Users.Activate",
                "Users.ChangeRole",
                "Users.ResetPassword",
                "Users.ManagePermissions",
                
                // Role Management
                "Roles.View",
                "Roles.Create",
                "Roles.Edit",
                "Roles.Delete",
                "Roles.ManagePermissions",
                
                // Permissions
                "Permissions.View",
                "Permissions.Assign",
                "Permissions.Revoke",
                
                // Patient Management
                "Patients.View",
                "Patients.ViewDetails",
                "Patients.Create",
                "Patients.Edit",
                "Patients.Delete",
                "Patients.ViewMedicalHistory",
                "Patients.EditMedicalHistory",
                "Patients.ViewDocuments",
                "Patients.UploadDocuments",
                "Patients.Export",
                
                // Appointments
                "Appointments.View",
                "Appointments.Create",
                "Appointments.Edit",
                "Appointments.Cancel",
                "Appointments.CheckIn",
                "Appointments.ViewCalendar",
                
                // Medical Records
                "MedicalRecords.View",
                "MedicalRecords.Create",
                "MedicalRecords.Edit",
                "MedicalRecords.Prescribe",
                "MedicalRecords.ViewPrescriptions",
                
                // Billing
                "Payments.View",
                "Payments.Create",
                "Payments.Edit",
                "Payments.Process",
                "Payments.Refund",
                "Payments.ViewReports",
                "Invoices.View",
                "Invoices.Create",
                "Invoices.Edit",
                "Invoices.Send",
                "Invoices.ViewReports",
                
                // Reports
                "Reports.ViewDashboard",
                "Reports.ViewPatientReports",
                "Reports.ViewFinancialReports",
                "Reports.ViewAppointmentReports",
                "Reports.Export",
                "Reports.Generate",
                
                // Settings
                "Settings.View",
                "Settings.Edit",
                
                // Audit Logs
                "AuditLogs.View",
                "AuditLogs.Export"
            },

            // ============================================
            // 👨‍⚕️ DOCTOR - Medical Care
            // ============================================
            ["Doctor"] = new[]
            {
                "Patients.View",
                "Patients.ViewDetails",
                "Patients.Create",
                "Patients.Edit",
                "Patients.ViewMedicalHistory",
                "Patients.EditMedicalHistory",
                "Patients.ViewDocuments",
                "Patients.UploadDocuments",
                "Appointments.View",
                "Appointments.Create",
                "Appointments.Edit",
                "Appointments.CheckIn",
                "Appointments.ViewCalendar",
                "MedicalRecords.View",
                "MedicalRecords.Create",
                "MedicalRecords.Edit",
                "MedicalRecords.Prescribe",
                "MedicalRecords.ViewPrescriptions",
                "Reports.ViewPatientReports"
            },

            // ============================================
            // 👩‍⚕️ NURSE - Patient Care Support
            // ============================================
            ["Nurse"] = new[]
            {
                "Patients.View",
                "Patients.ViewDetails",
                "Patients.ViewMedicalHistory",
                "Patients.ViewDocuments",
                "Appointments.View",
                "Appointments.CheckIn",
                "Appointments.ViewCalendar",
                "MedicalRecords.View",
                "MedicalRecords.ViewPrescriptions"
            },

            // ============================================
            // 📞 RECEPTIONIST - Front Desk
            // ============================================
            ["Receptionist"] = new[]
            {
                "Patients.View",
                "Patients.ViewDetails",
                "Patients.Create",
                "Patients.Edit",
                "Appointments.View",
                "Appointments.Create",
                "Appointments.Edit",
                "Appointments.Cancel",
                "Appointments.CheckIn",
                "Appointments.ViewCalendar",
                "Payments.View",
                "Payments.Create",
                "Invoices.View",
                "Invoices.Create",
                "Invoices.Send",
                "Users.View"
            }
        };
    }
}