using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// Seeds application permissions into the database.
/// Defines all available permissions across the system.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 18:46:03 UTC
/// </summary>
public class PermissionsSeeder : ISingleSeeder
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<PermissionsSeeder> _logger;

    public int Order => 2;

    public PermissionsSeeder(
        NabdCareDbContext dbContext,
        ILogger<PermissionsSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("🌱 Seeding permissions...");

        var permissions = GetGroupedPermissions();
        var addedCount = 0;

        foreach (var permission in permissions)
        {
            var exists = await _dbContext.AppPermissions
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Name == permission.Name);

            if (!exists)
            {
                permission.Id = Guid.NewGuid();
                permission.CreatedAt = DateTime.UtcNow;
                permission.CreatedBy = "System:Seeder";
                permission.IsDeleted = false;

                _dbContext.AppPermissions.Add(permission);
                addedCount++;

                _logger.LogDebug("   ➕ Added permission: {Name}", permission.Name);
            }
        }

        if (addedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ {Count} permissions seeded successfully", addedCount);
        }
        else
        {
            _logger.LogInformation("✅ All permissions already exist, skipping seed");
        }
    }

    private static List<AppPermission> GetGroupedPermissions()
    {
        return new List<AppPermission>
        {
            // ============================================
            // 🏢 CLINIC MANAGEMENT (SuperAdmin)
            // ============================================
            new() { Name = "Clinics.ViewAll", Description = "View all clinics in the system (SuperAdmin only)" },
            new() { Name = "Clinics.Create", Description = "Create new clinics (onboard new customers)" },
            new() { Name = "Clinics.Edit", Description = "Edit any clinic's information" },
            new() { Name = "Clinics.Delete", Description = "Soft delete clinics" },
            new() { Name = "Clinics.HardDelete", Description = "Permanently delete clinics (irreversible)" },
            new() { Name = "Clinics.ManageStatus", Description = "Activate/Suspend clinics" },
            new() { Name = "Clinics.ViewStats", Description = "View clinic statistics and metrics" },

            // ============================================
            // 🏥 OWN CLINIC MANAGEMENT (Clinic Admin)
            // ============================================
            new() { Name = "Clinic.View", Description = "View own clinic information" },
            new() { Name = "Clinic.Edit", Description = "Edit own clinic information" },
            new() { Name = "Clinic.ViewSettings", Description = "View clinic settings" },
            new() { Name = "Clinic.EditSettings", Description = "Edit clinic settings" },
            new() { Name = "Clinic.ManageBranches", Description = "Manage clinic branches" },

            // ============================================
            // 📋 SUBSCRIPTION MANAGEMENT
            // ============================================
            new() { Name = "Subscriptions.ViewAll", Description = "View all subscriptions (SuperAdmin only)" },
            new() { Name = "Subscriptions.View", Description = "View own clinic subscription" },
            new() { Name = "Subscriptions.Create", Description = "Create subscription plans (SuperAdmin only)" },
            new() { Name = "Subscriptions.Edit", Description = "Edit subscription details (SuperAdmin only)" },
            new() { Name = "Subscriptions.Cancel", Description = "Cancel subscriptions (SuperAdmin only)" },
            new() { Name = "Subscriptions.Renew", Description = "Renew subscriptions" },

            // ============================================
            // 👥 USER MANAGEMENT
            // ============================================
            new() { Name = "Users.View", Description = "View users list in own clinic" },
            new() { Name = "Users.ViewAll", Description = "View all users across all clinics (SuperAdmin only)" },
            new() { Name = "Users.ViewDetails", Description = "View detailed user information" },
            new() { Name = "Users.Create", Description = "Create new users" },
            new() { Name = "Users.Edit", Description = "Edit user information" },
            new() { Name = "Users.Delete", Description = "Delete users (soft delete)" },
            new() { Name = "Users.Activate", Description = "Activate/deactivate users" },
            new() { Name = "Users.ChangeRole", Description = "Change user roles" },
            new() { Name = "Users.ResetPassword", Description = "Reset user passwords" },
            new() { Name = "Users.ManagePermissions", Description = "Manage user-specific permissions" },

            // ============================================
            // 🔐 ROLE MANAGEMENT
            // ============================================
            new() { Name = "Roles.View", Description = "View roles in own clinic" },
            new() { Name = "Roles.ViewAll", Description = "View all roles including templates (SuperAdmin only)" },
            new() { Name = "Roles.Create", Description = "Create new roles" },
            new() { Name = "Roles.Edit", Description = "Edit role information" },
            new() { Name = "Roles.Delete", Description = "Delete roles" },
            new() { Name = "Roles.ManagePermissions", Description = "Assign/remove permissions from roles" },

            // ============================================
            // 🔑 PERMISSION MANAGEMENT
            // ============================================
            new() { Name = "Permissions.View", Description = "View all available permissions" },
            new() { Name = "Permissions.Assign", Description = "Assign permissions to roles/users" },
            new() { Name = "Permissions.Revoke", Description = "Revoke permissions from roles/users" },

            // ============================================
            // 🩺 PATIENT MANAGEMENT
            // ============================================
            new() { Name = "Patients.View", Description = "View patients list" },
            new() { Name = "Patients.ViewDetails", Description = "View detailed patient information" },
            new() { Name = "Patients.Create", Description = "Register new patients" },
            new() { Name = "Patients.Edit", Description = "Edit patient information" },
            new() { Name = "Patients.Delete", Description = "Delete patient records" },
            new() { Name = "Patients.ViewMedicalHistory", Description = "View patient medical history" },
            new() { Name = "Patients.EditMedicalHistory", Description = "Edit patient medical history" },
            new() { Name = "Patients.ViewDocuments", Description = "View patient documents" },
            new() { Name = "Patients.UploadDocuments", Description = "Upload patient documents" },
            new() { Name = "Patients.Export", Description = "Export patient data" },

            // ============================================
            // 📅 APPOINTMENTS
            // ============================================
            new() { Name = "Appointments.View", Description = "View appointments" },
            new() { Name = "Appointments.Create", Description = "Create new appointments" },
            new() { Name = "Appointments.Edit", Description = "Edit appointment details" },
            new() { Name = "Appointments.Cancel", Description = "Cancel appointments" },
            new() { Name = "Appointments.CheckIn", Description = "Check-in patients for appointments" },
            new() { Name = "Appointments.ViewCalendar", Description = "View appointment calendar" },

            // ============================================
            // 📋 MEDICAL RECORDS
            // ============================================
            new() { Name = "MedicalRecords.View", Description = "View medical records" },
            new() { Name = "MedicalRecords.Create", Description = "Create medical records" },
            new() { Name = "MedicalRecords.Edit", Description = "Edit medical records" },
            new() { Name = "MedicalRecords.Delete", Description = "Delete medical records" },
            new() { Name = "MedicalRecords.Prescribe", Description = "Issue prescriptions" },
            new() { Name = "MedicalRecords.ViewPrescriptions", Description = "View prescriptions" },

            // ============================================
            // 💰 BILLING & PAYMENTS
            // ============================================
            new() { Name = "Payments.View", Description = "View payment records" },
            new() { Name = "Payments.Create", Description = "Create payment records" },
            new() { Name = "Payments.Edit", Description = "Edit payment information" },
            new() { Name = "Payments.Delete", Description = "Delete payment records" },
            new() { Name = "Payments.Process", Description = "Process payments" },
            new() { Name = "Payments.Refund", Description = "Process refunds" },
            new() { Name = "Payments.ViewReports", Description = "View payment reports" },

            new() { Name = "Invoices.View", Description = "View invoices" },
            new() { Name = "Invoices.Create", Description = "Create invoices" },
            new() { Name = "Invoices.Edit", Description = "Edit invoices" },
            new() { Name = "Invoices.Send", Description = "Send invoices to patients" },
            new() { Name = "Invoices.ViewReports", Description = "View invoice reports" },

            // ============================================
            // 📊 REPORTS & ANALYTICS
            // ============================================
            new() { Name = "Reports.ViewDashboard", Description = "View analytics dashboard" },
            new() { Name = "Reports.ViewPatientReports", Description = "View patient reports" },
            new() { Name = "Reports.ViewFinancialReports", Description = "View financial reports" },
            new() { Name = "Reports.ViewAppointmentReports", Description = "View appointment reports" },
            new() { Name = "Reports.Export", Description = "Export reports" },
            new() { Name = "Reports.Generate", Description = "Generate custom reports" },

            // ============================================
            // ⚙️ SETTINGS
            // ============================================
            new() { Name = "Settings.View", Description = "View system settings" },
            new() { Name = "Settings.Edit", Description = "Edit system settings" },
            new() { Name = "Settings.ManageRoles", Description = "Manage all roles (SuperAdmin only)" }, // <-- ADDED LINE

            // ============================================
            // 📜 AUDIT LOGS
            // ============================================
            new() { Name = "AuditLogs.View", Description = "View audit logs" },
            new() { Name = "AuditLogs.Export", Description = "Export audit logs" },

            // ============================================
            // ⚙️ SYSTEM MANAGEMENT (SuperAdmin Only)
            // ============================================
            new() { Name = "System.ManageSettings", Description = "Manage system-wide settings (SuperAdmin only)" },
            new() { Name = "System.ViewLogs", Description = "View system logs (SuperAdmin only)" },
            new() { Name = "System.ManageRoles", Description = "Manage system roles and templates (SuperAdmin only)" }
        };
    }
}