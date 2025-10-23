using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Domain.Entities.Audits;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Interfaces;

namespace NabdCare.Infrastructure.Persistence;

/// <summary>
/// NabdCare database context with multi-tenant query filters and audit logging.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-23 20:01:28 UTC
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

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ChequePaymentDetail> ChequePaymentDetails { get; set; }
    public DbSet<AppPermission> AppPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NabdCareDbContext).Assembly);

        // ============================================
        // MULTI-TENANT QUERY FILTERS
        // ============================================

        // Role query filter
        modelBuilder.Entity<Role>().HasQueryFilter(r =>
            !r.IsDeleted &&
            (
                IsSuperAdminUser ||
                r.ClinicId == null ||
                (TenantId.HasValue && r.ClinicId == TenantId)
            )
        );

        // User query filter
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted && 
            (IsSuperAdminUser || (TenantId.HasValue && u.ClinicId == TenantId))
        );

        // Payment query filter
        modelBuilder.Entity<Payment>().HasQueryFilter(p =>
            !p.IsDeleted && 
            (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId))
        );

        // ChequePaymentDetail query filter
        modelBuilder.Entity<ChequePaymentDetail>().HasQueryFilter(cd =>
            !cd.IsDeleted && 
            !cd.Payment.IsDeleted &&
            (IsSuperAdminUser || (TenantId.HasValue && cd.Payment.ClinicId == TenantId))
        );

        // AuditLog query filter
        modelBuilder.Entity<AuditLog>().HasQueryFilter(a =>
            IsSuperAdminUser || (TenantId.HasValue && a.ClinicId == TenantId)
        );

        // ============================================
        // SIMPLE SOFT DELETE FILTERS
        // ============================================

        modelBuilder.Entity<Clinic>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<AppPermission>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(rp => !rp.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(up => !up.IsDeleted);

        // ============================================
        // GLOBAL DECIMAL PRECISION
        // ============================================

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        base.OnModelCreating(modelBuilder);
    }

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

    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;
        var userId = _userContext.GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
        }
    }

    private void HandleSoftDeleteAuditing()
    {
        var now = DateTime.UtcNow;
        var userId = _userContext.GetCurrentUserId();

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Modified && entry.Entity.IsDeleted)
            {
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = userId;
            }
        }
    }
}