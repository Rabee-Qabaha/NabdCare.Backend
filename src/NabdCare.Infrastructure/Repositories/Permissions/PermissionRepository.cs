using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Permissions;

public class PermissionRepository : IPermissionRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ITenantContext _tenant;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public PermissionRepository(
        NabdCareDbContext dbContext,
        ITenantContext tenant,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _dbContext = dbContext;
        _tenant = tenant;
        _cacheInvalidator = cacheInvalidator;
    }

    private bool IsSuperAdmin => _tenant.IsSuperAdmin;
    private Guid? TenantClinicId => _tenant.ClinicId;

    #region CRUD

    public async Task<IEnumerable<AppPermission>> GetAllPermissionsAsync()
        => await _dbContext.AppPermissions.ToListAsync();

    public async Task<AppPermission?> GetPermissionByIdAsync(Guid permissionId)
        => await _dbContext.AppPermissions.FindAsync(permissionId);

    public async Task<AppPermission> CreatePermissionAsync(AppPermission appPermission)
    {
        await _dbContext.AppPermissions.AddAsync(appPermission);
        await _dbContext.SaveChangesAsync();
        return appPermission;
    }

    public async Task<AppPermission?> UpdatePermissionAsync(Guid permissionId, AppPermission appPermission)
    {
        var existing = await _dbContext.AppPermissions.FindAsync(permissionId);
        if (existing == null) return null;

        existing.Name = appPermission.Name;
        existing.Description = appPermission.Description;

        _dbContext.AppPermissions.Update(existing);
        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeletePermissionAsync(Guid permissionId)
    {
        var existing = await _dbContext.AppPermissions.FindAsync(permissionId);
        if (existing == null) return false;

        _dbContext.AppPermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Role Permissions
    // ⚠️ These methods will be handled by RoleRepository for cache invalidation
    public async Task<IEnumerable<AppPermission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .Include(rp => rp.AppPermission)
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .Select(rp => rp.AppPermission)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (!IsSuperAdmin)
        {
            var role = await _dbContext.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null || role.ClinicId != TenantClinicId)
                throw new UnauthorizedAccessException("Cannot assign permissions to a role outside your clinic.");
        }

        if (await _dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted))
            return false;

        _dbContext.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenant.UserId?.ToString()
        });

        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateRoleAsync(roleId);

        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        if (!IsSuperAdmin)
        {
            var role = await _dbContext.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null || role.ClinicId != TenantClinicId)
                throw new UnauthorizedAccessException("Cannot modify a role outside your clinic.");
        }

        var existing = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted);

        if (existing == null) return false;

        _dbContext.RolePermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateRoleAsync(roleId);

        return true;
    }

    #endregion

    #region User Permissions

    public async Task<IEnumerable<AppPermission>> GetPermissionsByUserAsync(Guid userId)
    {
        if (!IsSuperAdmin)
        {
            var targetUser = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (targetUser == null || targetUser.ClinicId != TenantClinicId)
                throw new UnauthorizedAccessException("Cannot view permissions for a user outside your clinic.");
        }

        return await _dbContext.UserPermissions
            .Include(up => up.AppPermission)
            .Where(up => up.UserId == userId && !up.IsDeleted)
            .Select(up => up.AppPermission)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        if (!IsSuperAdmin)
        {
            var targetUser = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (targetUser == null || targetUser.ClinicId != TenantClinicId)
                throw new UnauthorizedAccessException("Cannot assign user permissions outside your clinic.");
        }

        if (await _dbContext.UserPermissions.AnyAsync(up => up.UserId == userId && up.PermissionId == permissionId && !up.IsDeleted))
            return false;

        _dbContext.UserPermissions.Add(new UserPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenant.UserId?.ToString()
        });

        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateUserAsync(userId);

        return true;
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        if (!IsSuperAdmin)
        {
            var targetUser = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (targetUser == null || targetUser.ClinicId != TenantClinicId)
                throw new UnauthorizedAccessException("Cannot modify user permissions outside your clinic.");
        }

        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId && !up.IsDeleted);

        if (existing == null) return false;

        _dbContext.UserPermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateUserAsync(userId);

        return true;
    }

    #endregion
}