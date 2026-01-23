using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Domain.Entities.Audits;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Invoices;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Entities.Patients; // ✅ New
using NabdCare.Domain.Entities.Inventory; // ✅ New
using NabdCare.Domain.Entities.Scheduling; // ✅ New
using NabdCare.Domain.Entities.Clinical; // ✅ New
using NabdCare.Domain.Interfaces;

namespace NabdCare.Infrastructure.Persistence;

/// <summary>
/// NabdCare database context with multi-tenant query filters and audit logging.
/// Author: Rabee-Qabaha
/// Updated: 2026-01-12
/// </summary>
public class NabdCareDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    private Guid? TenantId => _tenantContext.ClinicId;
    private bool IsSuperAdminUser => _tenantContext.IsSuperAdmin;

    public NabdCareDbContext(
        DbContextOptions<NabdCareDbContext> options,
        ITenantContext tenantContext,
        IUserContext userContext) : base(options)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    // ================================================================
    // DbSets (Core SaaS)
    // ================================================================
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Clinic> Clinics { get; set; } = default!;
    public DbSet<Subscription> Subscriptions { get; set; } = default!;
    public DbSet<Branch> Branches { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;
    public DbSet<ChequePaymentDetail> ChequePaymentDetails { get; set; } = default!;
    public DbSet<AppPermission> AppPermissions { get; set; } = default!;
    public DbSet<RolePermission> RolePermissions { get; set; } = default!;
    public DbSet<UserPermission> UserPermissions { get; set; } = default!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;
    public DbSet<AuditLog> AuditLogs { get; set; } = default!;
    
    public DbSet<Invoice> Invoices { get; set; } = default!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = default!;

    // ================================================================
    // DbSets (New Modules: Clinical, Inventory, Scheduling)
    // ================================================================
    public DbSet<Patient> Patients { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<Appointment> Appointments { get; set; } = default!;
    public DbSet<PractitionerSchedule> PractitionerSchedules { get; set; } = default!;
    public DbSet<ClinicalEncounter> ClinicalEncounters { get; set; } = default!;
    public DbSet<Prescription> Prescriptions { get; set; } = default!;
    public DbSet<PatientDocument> PatientDocuments { get; set; } = default!;
    public DbSet<ClinicalTemplate> ClinicalTemplates { get; set; } = default!; // Optional UI Configs

    // ================================================================
    // MODEL CONFIGURATION
    // ================================================================
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ✅ Apply entity configurations (includes InvoiceConfiguration, etc.)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NabdCareDbContext).Assembly);

        // ============================================================
        // ✅ MULTI-TENANT QUERY FILTERS
        // ============================================================

        // ✅ AppPermission filter (Simple Soft Delete)
        modelBuilder.Entity<AppPermission>().HasQueryFilter(ap => !ap.IsDeleted);

        // ✅ Subscription filter
        modelBuilder.Entity<Subscription>().HasQueryFilter(s =>
            !s.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && s.ClinicId == TenantId))
        );

        // ✅ Branch filter
        modelBuilder.Entity<Branch>().HasQueryFilter(b =>
            !b.IsDeleted &&
            (
                IsSuperAdminUser || 
                (TenantId.HasValue && b.ClinicId == TenantId)
            )
        );
        
        // ✅ Role filter
        modelBuilder.Entity<Role>().HasQueryFilter(r =>
            !r.IsDeleted &&
            (
                IsSuperAdminUser ||
                r.ClinicId == null ||
                (TenantId.HasValue && r.ClinicId == TenantId)
            )
        );

        // ✅ User filter
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && u.ClinicId == TenantId))
        );

        // ✅ Payment filter
        modelBuilder.Entity<Payment>().HasQueryFilter(p =>
            !p.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId))
        );

        // ✅ ChequePaymentDetail filter
        modelBuilder.Entity<ChequePaymentDetail>().HasQueryFilter(cd =>
            !cd.IsDeleted &&
            !cd.Payment.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && cd.Payment.ClinicId == TenantId))
        );

        // ✅ Invoice filter
        modelBuilder.Entity<Invoice>().HasQueryFilter(i =>
            !i.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && i.ClinicId == TenantId))
        );

        // ✅ InvoiceItem filter
        modelBuilder.Entity<InvoiceItem>().HasQueryFilter(ii =>
            !ii.IsDeleted &&
            !ii.Invoice.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && ii.Invoice.ClinicId == TenantId))
        );

        // ✅ RolePermission filter
        modelBuilder.Entity<RolePermission>().HasQueryFilter(rp =>
            !rp.IsDeleted &&
            (
                IsSuperAdminUser ||
                (
                    TenantId.HasValue &&
                    (
                        rp.Role.ClinicId == TenantId || // role belongs to this clinic
                        (rp.Role.ClinicId == null && rp.Role.IsTemplate) // global template
                    )
                )
            )
        );

        // ✅ UserPermission filter
        modelBuilder.Entity<UserPermission>().HasQueryFilter(up =>
            !up.IsDeleted &&
            (
                IsSuperAdminUser ||
                (
                    TenantId.HasValue &&
                    up.User.ClinicId == TenantId
                )
            )
        );

        // --- NEW MODULE FILTERS ---

        // ✅ Patient filter
        modelBuilder.Entity<Patient>().HasQueryFilter(p =>
            !p.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId))
        );

        // ✅ Product filter
        modelBuilder.Entity<Product>().HasQueryFilter(p =>
            !p.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId))
        );

        // ✅ Appointment filter
        modelBuilder.Entity<Appointment>().HasQueryFilter(a =>
            !a.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && a.ClinicId == TenantId))
        );

        // ✅ PractitionerSchedule filter
        modelBuilder.Entity<PractitionerSchedule>().HasQueryFilter(s =>
            !s.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && s.ClinicId == TenantId))
        );

        // ✅ ClinicalEncounter filter
        modelBuilder.Entity<ClinicalEncounter>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && e.ClinicId == TenantId))
        );

        // ✅ Prescription filter
        modelBuilder.Entity<Prescription>().HasQueryFilter(p =>
            !p.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId))
        );

        // ✅ PatientDocument filter
        modelBuilder.Entity<PatientDocument>().HasQueryFilter(d =>
            !d.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && d.ClinicId == TenantId))
        );
        
        // ✅ ClinicalTemplate filter
        modelBuilder.Entity<ClinicalTemplate>().HasQueryFilter(t =>
            !t.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && t.ClinicId == TenantId))
        );

        // ============================================================
        // ✅ Soft Delete + Decimal Precision Normalization
        // ============================================================
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        base.OnModelCreating(modelBuilder);
    }

    // ================================================================
    // AUDIT + SOFT DELETE LOGIC
    // ================================================================
    public override int SaveChanges()
    {
        SetAuditFields();
        HandleSoftDeleteAuditing();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        HandleSoftDeleteAuditing();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically sets CreatedBy / UpdatedBy audit fields for auditable entities.
    /// </summary>
    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;
        var userId = _userContext.GetCurrentUserId();
        var userFullName = _userContext.GetCurrentUserFullName();

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            var userIdentifier = $"{userId}|{userFullName}";

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userIdentifier;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userIdentifier;
            }
        }
    }

    /// <summary>
    /// Handles deleted auditing for soft deletable entities.
    /// </summary>
    private void HandleSoftDeleteAuditing()
    {
        var now = DateTime.UtcNow;
        var userId = _userContext.GetCurrentUserId();
        var userFullName = _userContext.GetCurrentUserFullName();
        var userIdentifier = $"{userId}|{userFullName}";

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Modified && entry.Entity.IsDeleted)
            {
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = userIdentifier;
            }
        }
    }
}