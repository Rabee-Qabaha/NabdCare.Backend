using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Payments;
using NabdCare.Domain.Entities.Users;
using NabdCare.Domain.Interfaces;

namespace NabdCare.Infrastructure.Persistence;

public class NabdCareDbContext : DbContext
{
    public Guid? TenantId { get; set; }
    public bool IsSuperAdminUser { get; set; }

    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public NabdCareDbContext(
        DbContextOptions<NabdCareDbContext> options,
        ITenantContext tenantContext,
        IUserContext userContext) : base(options)
    {
        _tenantContext = tenantContext;
        _userContext = userContext;

        TenantId = tenantContext.ClinicId;
        IsSuperAdminUser = tenantContext.IsSuperAdmin;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ChequePaymentDetail> ChequePaymentDetails { get; set; }
    public DbSet<AppPermission> AppPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations (indexes, relationships, constraints)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NabdCareDbContext).Assembly);

        // Global query filters for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted && (IsSuperAdminUser || (TenantId.HasValue && u.ClinicId == TenantId)));

        modelBuilder.Entity<Payment>().HasQueryFilter(p =>
            !p.IsDeleted && (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId)));

        // Global query filters for soft delete & multi-tenancy
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted && (IsSuperAdminUser || (TenantId.HasValue && u.ClinicId == TenantId)));
        modelBuilder.Entity<Payment>().HasQueryFilter(p => !p.IsDeleted && (IsSuperAdminUser || (TenantId.HasValue && p.ClinicId == TenantId)));
        modelBuilder.Entity<Clinic>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<AppPermission>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(rp => !rp.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(up => !up.IsDeleted);

        // --- Global decimal precision convention ---
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
