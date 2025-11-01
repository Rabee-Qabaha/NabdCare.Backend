using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.Interfaces;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Infrastructure.Persistence.DataSeed;

/// <summary>
/// 🌱 Seeds default role-permission mappings for the system.
/// Automatically links roles (SuperAdmin, ClinicAdmin, Doctor, Receptionist)
/// with permissions defined in <see cref="Permissions"/>.
/// 
/// Works dynamically with seeded AppPermissions, ensuring consistency
/// even when new permissions are added later.
/// 
/// Author: Rabee Qabaha
/// Updated: 2025-10-31
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
        // 🔹 SUPER ADMIN — Full Access
        // ===============================
        var superAdminRole = roles.FirstOrDefault(r => r.Name == "SuperAdmin");
        if (superAdminRole != null)
            await AssignAllPermissionsAsync(superAdminRole, appPermissions);

        // ===============================
        // 🔹 CLINIC ADMIN — Limited System Access
        // ===============================
        var clinicAdminRole = roles.FirstOrDefault(r => r.Name == "ClinicAdmin");
        if (clinicAdminRole != null)
        {
            await AssignPermissionsAsync(clinicAdminRole, appPermissions, new[]
            {
                // Clinics
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
                Permissions.Reports.Export
            });
        }

        // ===============================
        // 🔹 DOCTOR — Patient + Appointment Focus
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
        // 🔹 RECEPTIONIST — Appointments + Patients
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
                Permissions.Patients.View,
                Permissions.Patients.Create,
                Permissions.Patients.Edit
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
            _logger.LogInformation("🧩 Assigned {Count} permissions to SuperAdmin.", missing.Count);
        }
        else
        {
            _logger.LogInformation("🟢 SuperAdmin already has all permissions.");
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
            _logger.LogInformation("🟢 Role {Role} already up-to-date", role.Name);
        }
    }
}