using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Permissions;

public class PermissionRepository : IPermissionRepository
{
    private readonly NabdCareDbContext _dbContext;

    public PermissionRepository(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

    public async Task<IEnumerable<AppPermission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .Include(rp => rp.AppPermission)
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.AppPermission)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (await _dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            return false;

        _dbContext.RolePermissions.Add(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var existing = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (existing == null) return false;

        _dbContext.RolePermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    #endregion

    #region User Permissions

    public async Task<IEnumerable<AppPermission>> GetPermissionsByUserAsync(Guid userId)
    {
        return await _dbContext.UserPermissions
            .Include(up => up.AppPermission)
            .Where(up => up.UserId == userId)
            .Select(up => up.AppPermission)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        if (await _dbContext.UserPermissions.AnyAsync(up => up.UserId == userId && up.PermissionId == permissionId))
            return false;

        _dbContext.UserPermissions.Add(new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId
        });

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing == null) return false;

        _dbContext.UserPermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    #endregion
}