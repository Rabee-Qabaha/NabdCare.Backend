using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Roles;

public class RoleRepository : IRoleRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(NabdCareDbContext dbContext, ILogger<RoleRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    #region QUERY METHODS

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        return await _dbContext.Roles
            .Include(r => r.Clinic)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetSystemRolesAsync()
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters() // System roles have no clinic
            .Where(r => r.IsSystemRole)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetTemplateRolesAsync()
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters() // Templates have no clinic
            .Where(r => r.IsTemplate)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetClinicRolesAsync(Guid clinicId)
    {
        return await _dbContext.Roles
            .Where(r => r.ClinicId == clinicId)
            .Include(r => r.Clinic)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Role?> GetRoleByIdAsync(Guid id)
    {
        return await _dbContext.Roles
            .Include(r => r.Clinic)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Role?> GetRoleByNameAsync(string name, Guid? clinicId = null)
    {
        return await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == name && r.ClinicId == clinicId);
    }

    public async Task<int> GetRoleUserCountAsync(Guid roleId)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters() // Count all users, even deleted
            .CountAsync(u => u.RoleId == roleId && !u.IsDeleted);
    }

    public async Task<int> GetRolePermissionCountAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .CountAsync(rp => rp.RoleId == roleId && !rp.IsDeleted);
    }

    public async Task<bool> RoleExistsAsync(Guid id)
    {
        return await _dbContext.Roles.AnyAsync(r => r.Id == id);
    }

    public async Task<bool> RoleNameExistsAsync(string name, Guid? clinicId, Guid? excludeRoleId = null)
    {
        var query = _dbContext.Roles
            .Where(r => r.Name == name && r.ClinicId == clinicId);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region COMMAND METHODS

    public async Task<Role> CreateRoleAsync(Role role)
    {
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<Role?> UpdateRoleAsync(Role role)
    {
        _dbContext.Roles.Update(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await GetRoleByIdAsync(id);
        if (role == null)
            return false;

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    #endregion

    #region PERMISSION MANAGEMENT

    public async Task<IEnumerable<Guid>> GetRolePermissionIdsAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        // Check if already exists
        var exists = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted);

        if (exists)
            return false;

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsDeleted = false
        };

        _dbContext.RolePermissions.Add(rolePermission);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted);

        if (rolePermission == null)
            return false;

        _dbContext.RolePermissions.Remove(rolePermission);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        var existingPermissionIds = await GetRolePermissionIdsAsync(roleId);
        var newPermissionIds = permissionIds.Except(existingPermissionIds).ToList();

        if (!newPermissionIds.Any())
            return 0;

        var rolePermissions = newPermissionIds.Select(permissionId => new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsDeleted = false
        }).ToList();

        _dbContext.RolePermissions.AddRange(rolePermissions);
        await _dbContext.SaveChangesAsync();

        return rolePermissions.Count;
    }

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        // Get existing permissions
        var existing = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .ToListAsync();

        // Remove all existing
        _dbContext.RolePermissions.RemoveRange(existing);

        // Add new permissions
        var rolePermissions = permissionIds.Select(permissionId => new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            IsDeleted = false
        }).ToList();

        _dbContext.RolePermissions.AddRange(rolePermissions);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    #endregion
}