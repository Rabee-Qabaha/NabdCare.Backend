using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.Common.Constants;

// ===============================================================
// File: /NabdCare.Application/Common/Constants/Permissions.cs
// Author: Rabee Qabaha
// Updated: 2025-10-31
// Purpose: Centralized definitions for all permission constants
// ===============================================================
public static class Permissions
{
    // ============================================
    // 🏢 CLINICS (System-level)
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Clinics
    {
        public const string ViewAll = "Clinics.ViewAll";
        public const string ViewActive = "Clinics.ViewActive";
        public const string Search = "Clinics.Search";
        public const string Create = "Clinics.Create";
        public const string Edit = "Clinics.Edit";
        public const string Delete = "Clinics.Delete";
        public const string HardDelete = "Clinics.HardDelete";
        public const string ManageStatus = "Clinics.ManageStatus";
        public const string ViewStats = "Clinics.ViewStats";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [ViewAll] = "View all clinics in the system (SuperAdmin only)",
            [ViewActive] = "View only active clinics",
            [Search] = "Search clinics by name, email, or phone",
            [Create] = "Create new clinics (onboard new customers)",
            [Edit] = "Edit clinic details",
            [Delete] = "Soft delete a clinic record",
            [HardDelete] = "Permanently delete a clinic (irreversible)",
            [ManageStatus] = "Activate or suspend clinic accounts",
            [ViewStats] = "View overall clinic performance metrics"
        };
    }

    // ============================================
    // 🏥 OWN CLINIC MANAGEMENT
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Clinic
    {
        public const string View = "Clinic.View";
        public const string Edit = "Clinic.Edit";
        public const string ViewSettings = "Clinic.ViewSettings";
        public const string EditSettings = "Clinic.EditSettings";
        public const string ManageBranches = "Clinic.ManageBranches";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View own clinic information",
            [Edit] = "Edit clinic details and contact information",
            [ViewSettings] = "View clinic configuration settings",
            [EditSettings] = "Modify clinic configuration settings",
            [ManageBranches] = "Add, edit, or remove clinic branches"
        };
    }

    // ============================================
    // 💳 SUBSCRIPTIONS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Subscriptions
    {
        public const string ViewAll = "Subscriptions.ViewAll";
        public const string View = "Subscriptions.View";
        public const string Create = "Subscriptions.Create";
        public const string Edit = "Subscriptions.Edit";
        public const string Delete = "Subscriptions.Delete";
        public const string HardDelete = "Subscriptions.HardDelete";
        public const string ChangeStatus = "Subscriptions.ChangeStatus";
        public const string Renew = "Subscriptions.Renew";
        public const string ViewActive = "Subscriptions.ViewActive";
        public const string ToggleAutoRenew = "Subscriptions.ToggleAutoRenew";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View subscriptions belonging to own clinic or others (if SuperAdmin)",
            [ViewActive] = "View only the active subscription for a clinic",
            [ViewAll] = "View all subscriptions across all clinics (SuperAdmin only)",
            [Create] = "Create new subscriptions (SuperAdmin only)",
            [Edit] = "Edit existing subscriptions",
            [Delete] = "Soft delete (cancel) a subscription",
            [HardDelete] = "Permanently delete a subscription (SuperAdmin only)",
            [ChangeStatus] = "Change subscription status (SuperAdmin only)",
            [Renew] = "Renew a subscription for a clinic (SuperAdmin only)",
            [ToggleAutoRenew] = "Enable or disable auto-renew for a subscription"
        };
    }

    // ============================================
    // 👥 USERS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Users
    {
        public const string View = "Users.View";
        public const string ViewAll = "Users.ViewAll";
        public const string ViewDetails = "Users.ViewDetails";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string HardDelete = "Users.HardDelete";
        public const string Activate = "Users.Activate";
        public const string ChangeRole = "Users.ChangeRole";
        public const string ResetPassword = "Users.ResetPassword";
        public const string ManagePermissions = "Users.ManagePermissions";
        public const string Restore = "Users.Restore";
        
        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View users in own clinic",
            [ViewAll] = "View all users across clinics (SuperAdmin only)",
            [ViewDetails] = "View full user information",
            [Create] = "Create new user accounts",
            [Edit] = "Edit existing user profiles",
            [Delete] = "Deactivate or soft delete users",
            [HardDelete] = "Permanently delete user accounts",
            [Activate] = "Activate or deactivate users",
            [ChangeRole] = "Change user roles",
            [ResetPassword] = "Reset user passwords",
            [ManagePermissions] = "Assign or revoke individual permissions",
        };
    }

    // ============================================
    // 🧩 ROLE MANAGEMENT PERMISSIONS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Roles
    {
        public const string ViewAll = "Roles.ViewAll";
        public const string ViewSystem = "Roles.ViewSystem";
        public const string ViewTemplates = "Roles.ViewTemplates";
        public const string ViewClinic = "Roles.ViewClinic";
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Clone = "Roles.Clone";
        public const string Edit = "Roles.Edit";
        public const string Delete = "Roles.Delete";
        public const string HardDelete = "Roles.HardDelete";
        public const string Restore = "Roles.Restore";
        
        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [ViewAll] = "View all roles (system + clinic + templates)",
            [ViewSystem] = "View system roles (SuperAdmin only)",
            [ViewTemplates] = "View template roles for cloning",
            [ViewClinic] = "View roles of a specific clinic",
            [View] = "View a specific role by ID",
            [Create] = "Create new roles (clinic-level)",
            [Clone] = "Clone template roles to clinic",
            [Edit] = "Edit existing roles",
            [Delete] = "Delete clinic roles (cannot delete system roles)",
            [HardDelete] = "Permanently delete a role (irreversible)"
        };
    }

    // ============================================
    // 🔑 PERMISSIONS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class AppPermissions
    {
        public const string View = "AppPermissions.View";
        public const string ViewOwn = "AppPermissions.ViewOwn";
        public const string ViewUserPermissions = "AppPermissions.ViewUserPermissions";
        public const string Create = "AppPermissions.Create";
        public const string Edit = "AppPermissions.Edit";
        public const string Delete = "AppPermissions.Delete";
        public const string Manage = "AppPermissions.Manage";
        public const string Assign = "AppPermissions.Assign";
        public const string Revoke = "AppPermissions.Revoke";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View all permissions in the system",
            [ViewOwn] = "View own permissions (current user)",
            [ViewUserPermissions] = "View permissions assigned to another user",
            [Create] = "Create new permission records (SuperAdmin only)",
            [Edit] = "Edit existing permissions",
            [Delete] = "Delete permission definitions (SuperAdmin only)",
            [Manage] = "Manage permissions cache and synchronization",
            [Assign] = "Assign permissions to roles or users",
            [Revoke] = "Revoke permissions from roles or users"
        };
    }

    // ============================================
    // 🩺 PATIENTS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Patients
    {
        public const string View = "Patients.View";
        public const string ViewDetails = "Patients.ViewDetails";
        public const string Create = "Patients.Create";
        public const string Edit = "Patients.Edit";
        public const string Delete = "Patients.Delete";
        public const string HardDelete = "Patients.HardDelete";
        public const string ViewMedicalHistory = "Patients.ViewMedicalHistory";
        public const string EditMedicalHistory = "Patients.EditMedicalHistory";
        public const string ViewDocuments = "Patients.ViewDocuments";
        public const string UploadDocuments = "Patients.UploadDocuments";
        public const string Export = "Patients.Export";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View patient list",
            [ViewDetails] = "View detailed patient information",
            [Create] = "Register new patients",
            [Edit] = "Edit patient data",
            [Delete] = "Soft delete patient records",
            [HardDelete] = "Permanently delete patient records",
            [ViewMedicalHistory] = "View patient medical history",
            [EditMedicalHistory] = "Edit patient medical history",
            [ViewDocuments] = "View uploaded patient documents",
            [UploadDocuments] = "Upload new patient documents",
            [Export] = "Export patient data for reporting"
        };
    }

    // ============================================
    // 📅 APPOINTMENTS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Appointments
    {
        public const string View = "Appointments.View";
        public const string Create = "Appointments.Create";
        public const string Edit = "Appointments.Edit";
        public const string Cancel = "Appointments.Cancel";
        public const string CheckIn = "Appointments.CheckIn";
        public const string ViewCalendar = "Appointments.ViewCalendar";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View appointments list",
            [Create] = "Create new appointments",
            [Edit] = "Edit appointment details",
            [Cancel] = "Cancel scheduled appointments",
            [CheckIn] = "Check in patients for appointments",
            [ViewCalendar] = "View appointment calendar"
        };
    }

    // ============================================
    // 📋 MEDICAL RECORDS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class MedicalRecords
    {
        public const string View = "MedicalRecords.View";
        public const string Create = "MedicalRecords.Create";
        public const string Edit = "MedicalRecords.Edit";
        public const string Delete = "MedicalRecords.Delete";
        public const string HardDelete = "MedicalRecords.HardDelete";
        public const string Prescribe = "MedicalRecords.Prescribe";
        public const string ViewPrescriptions = "MedicalRecords.ViewPrescriptions";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View medical records",
            [Create] = "Create new medical records",
            [Edit] = "Edit existing medical records",
            [Delete] = "Soft delete medical records",
            [HardDelete] = "Permanently delete medical records",
            [Prescribe] = "Create or issue prescriptions",
            [ViewPrescriptions] = "View issued prescriptions"
        };
    }

    // ============================================
    // 💰 PAYMENTS & INVOICES
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Payments
    {
        public const string View = "Payments.View";
        public const string Create = "Payments.Create";
        public const string Edit = "Payments.Edit";
        public const string Delete = "Payments.Delete";
        public const string HardDelete = "Payments.HardDelete";
        public const string Process = "Payments.Process";
        public const string Refund = "Payments.Refund";
        public const string ViewReports = "Payments.ViewReports";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View payment records",
            [Create] = "Create payment entries",
            [Edit] = "Edit payment information",
            [Delete] = "Soft delete payment records",
            [HardDelete] = "Permanently delete payment records",
            [Process] = "Process new payments",
            [Refund] = "Issue payment refunds",
            [ViewReports] = "View financial payment reports"
        };
    }

    [ExportTsClass(OutputDir = "constants")]
    public static class Invoices
    {
        public const string View = "Invoices.View";
        public const string Create = "Invoices.Create";
        public const string Edit = "Invoices.Edit";
        public const string Send = "Invoices.Send";
        public const string ViewReports = "Invoices.ViewReports";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View invoices",
            [Create] = "Create new invoices",
            [Edit] = "Edit invoice details",
            [Send] = "Send invoices to patients",
            [ViewReports] = "View invoice reports"
        };
    }

    // ============================================
    // 📊 REPORTS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Reports
    {
        public const string ViewDashboard = "Reports.ViewDashboard";
        public const string ViewPatientReports = "Reports.ViewPatientReports";
        public const string ViewFinancialReports = "Reports.ViewFinancialReports";
        public const string ViewAppointmentReports = "Reports.ViewAppointmentReports";
        public const string Export = "Reports.Export";
        public const string Generate = "Reports.Generate";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [ViewDashboard] = "View analytics dashboard",
            [ViewPatientReports] = "View patient-related reports",
            [ViewFinancialReports] = "View financial reports",
            [ViewAppointmentReports] = "View appointment statistics reports",
            [Export] = "Export reports to external formats",
            [Generate] = "Generate new analytical reports"
        };
    }

    // ============================================
    // ⚙️ SETTINGS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class Settings
    {
        public const string View = "Settings.View";
        public const string Edit = "Settings.Edit";
        public const string ManageRoles = "Settings.ManageRoles";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View system settings",
            [Edit] = "Edit configuration settings",
            [ManageRoles] = "Manage role configurations (SuperAdmin only)"
        };
    }

    // ============================================
    // 📜 AUDIT LOGS
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class AuditLogs
    {
        public const string View = "AuditLogs.View";
        public const string Export = "AuditLogs.Export";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [View] = "View system and clinic audit logs",
            [Export] = "Export audit logs as CSV or Excel"
        };
    }

    // ============================================
    // 🧰 SYSTEM (SuperAdmin Only)
    // ============================================
    [ExportTsClass(OutputDir = "constants")]
    public static class System
    {
        public const string ManageSettings = "System.ManageSettings";
        public const string ViewLogs = "System.ViewLogs";
        public const string ManageRoles = "System.ManageRoles";

        public static readonly Dictionary<string, string> Descriptions = new()
        {
            [ManageSettings] = "Manage system-wide configuration settings",
            [ViewLogs] = "View application and infrastructure logs",
            [ManageRoles] = "Manage system and template roles"
        };
    }
}