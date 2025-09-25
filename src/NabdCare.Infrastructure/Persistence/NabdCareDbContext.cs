using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Domain.Entities.Clinic;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Interfaces;

namespace NabdCare.Infrastructure.Persistence;

public class NabdCareDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public NabdCareDbContext(DbContextOptions<NabdCareDbContext> options, ITenantContext tenantContext, IUserContext userContext) : base(options)
    {
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<ClinicPayment> ClinicPayments { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global query filter for ClinicId (multi-tenancy)
        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted &&
            (
                _tenantContext.IsSuperAdmin ||
                (_tenantContext.ClinicId.HasValue && u.ClinicId == _tenantContext.ClinicId)
            )
        );
        modelBuilder.Entity<ClinicPayment>().HasQueryFilter(cp =>
            !cp.IsDeleted &&
            (
                _tenantContext.IsSuperAdmin ||
                (_tenantContext.ClinicId.HasValue && cp.ClinicId == _tenantContext.ClinicId)
            )
        );

        // Soft delete filter for all entities
        modelBuilder.Entity<Clinic>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Permission>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(rp => !rp.IsDeleted);
        modelBuilder.Entity<UserPermission>().HasQueryFilter(up => !up.IsDeleted);

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
        var entries = ChangeTracker.Entries<IAuditable>();
        var now = DateTime.UtcNow;
        var userId = _userContext.GetCurrentUserId(); // Fetch current user ID

        foreach (var entry in entries)
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

    private void HandleSoftDeleteAuditing()
    {
        var entries = ChangeTracker.Entries<ISoftDeletable>();
        var now = DateTime.UtcNow;
        var userId = _userContext.GetCurrentUserId(); // Fetch current user ID

        foreach (var entry in entries)
            if (entry.State == EntityState.Modified && entry.Entity.IsDeleted)
            {
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = userId;
            }
    }
}