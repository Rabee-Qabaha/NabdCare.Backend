using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Roles;

public class RoleRepository : IRoleRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RoleRepository> _logger;
    private readonly IPermissionCacheInvalidator _cacheInvalidator; // 🧩 added

    public RoleRepository(
        NabdCareDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<RoleRepository> logger,
        IPermissionCacheInvalidator cacheInvalidator) // 🧩 added
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    private bool IsSuperAdmin() => _tenantContext.IsSuperAdmin;
    private Guid? CurrentClinicId => _tenantContext.ClinicId;
    private string? CurrentUserId => _tenantContext.UserId?.ToString();

    // ============================================
    // QUERY METHODS
    // ============================================

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
            .IgnoreQueryFilters()
            .Where(r => r.IsSystemRole)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetTemplateRolesAsync()
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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
            query = query.Where(r => r.Id != excludeRoleId.Value);

        return await query.AnyAsync();
    }

    // ============================================
    // COMMAND METHODS
    // ============================================

    public async Task<Role> CreateRoleAsync(Role role)
    {
        await _dbContext.Roles.AddAsync(role);
        await _dbContext.SaveChangesAsync();
        return role;
    }

    public async Task<Role?> UpdateRoleAsync(Role role)
    {
        var existing = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == role.Id);

        if (existing == null)
            return null;

        if (existing.IsSystemRole && !IsSuperAdmin())
            throw new UnauthorizedAccessException("System roles can only be modified by SuperAdmin");

        if (!IsSuperAdmin())
        {
            if (existing.ClinicId == null || existing.ClinicId != CurrentClinicId)
                throw new UnauthorizedAccessException("You can modify only roles belonging to your clinic");
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(role);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = CurrentUserId;

        await _dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return false;

        if (role.IsSystemRole && !IsSuperAdmin())
            throw new UnauthorizedAccessException("System roles cannot be deleted");

        if (!IsSuperAdmin() && role.ClinicId != CurrentClinicId)
            throw new UnauthorizedAccessException("You can delete only roles belonging to your clinic");

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        role.DeletedBy = CurrentUserId;

        _dbContext.Roles.Update(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // ============================================
    // PERMISSION MANAGEMENT
    // ============================================

    public async Task<IEnumerable<Guid>> GetRolePermissionIdsAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var existing = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (existing != null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = CurrentUserId;
                _dbContext.RolePermissions.Update(existing);
                await _dbContext.SaveChangesAsync();
            }

            await _cacheInvalidator.InvalidateRoleAsync(roleId); // 🧩 Invalidate cache
            return false;
        }

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserId,
            IsDeleted = false
        };

        await _dbContext.RolePermissions.AddAsync(rolePermission);
        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateRoleAsync(roleId); // 🧩 Invalidate cache

        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
            return false;

        rolePermission.IsDeleted = true;
        rolePermission.DeletedAt = DateTime.UtcNow;
        rolePermission.DeletedBy = CurrentUserId;

        _dbContext.RolePermissions.Update(rolePermission);
        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateRoleAsync(roleId); // 🧩 Invalidate cache

        return true;
    }

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        if (!IsSuperAdmin())
        {
            var role = await _dbContext.Roles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null || role.ClinicId != CurrentClinicId)
                throw new UnauthorizedAccessException("You can assign permissions only to roles in your clinic");
        }

        var existing = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        var newPermissionIds = permissionIds
            .Except(existing.Select(x => x.PermissionId))
            .ToList();

        foreach (var permissionId in newPermissionIds)
        {
            var rp = new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _dbContext.RolePermissions.AddAsync(rp);
        }

        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateRoleAsync(roleId); // 🧩 Invalidate cache

        return newPermissionIds.Count;
    }

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        if (!IsSuperAdmin())
        {
            var role = await _dbContext.Roles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null || role.ClinicId != CurrentClinicId)
                throw new UnauthorizedAccessException("You can modify permissions only for roles in your clinic");
        }

        var existing = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        foreach (var rp in existing)
        {
            rp.IsDeleted = true;
            rp.DeletedAt = DateTime.UtcNow;
            rp.DeletedBy = CurrentUserId;
        }

        _dbContext.RolePermissions.UpdateRange(existing);

        foreach (var permissionId in permissionIds)
        {
            var rp = existing.FirstOrDefault(x => x.PermissionId == permissionId);

            if (rp != null)
            {
                rp.IsDeleted = false;
                rp.UpdatedAt = DateTime.UtcNow;
                rp.UpdatedBy = CurrentUserId;
            }
            else
            {
                await _dbContext.RolePermissions.AddAsync(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CurrentUserId,
                    IsDeleted = false
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        await _cacheInvalidator.InvalidateRoleAsync(roleId); // 🧩 Invalidate cache

        return true;
    }
}