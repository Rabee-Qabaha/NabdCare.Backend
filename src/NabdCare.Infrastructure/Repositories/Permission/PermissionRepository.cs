using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Permission;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.User;
using NabdCare.Domain.Enums;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Users;

public class PermissionRepository : IPermissionRepository
{
    private readonly NabdCareDbContext _dbContext;

    public PermissionRepository(NabdCareDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        => await _dbContext.Permissions.ToListAsync();

    public async Task<Permission?> GetPermissionByIdAsync(Guid permissionId)
        => await _dbContext.Permissions.FindAsync(permissionId);

    public async Task<Permission> CreatePermissionAsync(Permission permission)
    {
        await _dbContext.Permissions.AddAsync(permission);
        await _dbContext.SaveChangesAsync();
        return permission;
    }

    public async Task<Permission?> UpdatePermissionAsync(Guid permissionId, Permission permission)
    {
        var existing = await _dbContext.Permissions.FindAsync(permissionId);
        if (existing == null) return null;

        existing.Name = permission.Name;
        existing.Description = permission.Description;

        _dbContext.Permissions.Update(existing);
        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeletePermissionAsync(Guid permissionId)
    {
        var existing = await _dbContext.Permissions.FindAsync(permissionId);
        if (existing == null) return false;

        _dbContext.Permissions.Remove(existing);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(UserRole role)
    {
        return await _dbContext.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.Role == role)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByUserAsync(Guid userId)
    {
        return await _dbContext.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId)
            .Select(up => up.Permission)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToRoleAsync(UserRole role, Guid permissionId)
    {
        if (await _dbContext.RolePermissions.AnyAsync(rp => rp.Role == role && rp.PermissionId == permissionId))
            return false;

        _dbContext.RolePermissions.Add(new RolePermission
        {
            Role = role,
            PermissionId = permissionId
        });

        await _dbContext.SaveChangesAsync();
        return true;
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
}
