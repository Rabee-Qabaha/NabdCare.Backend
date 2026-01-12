using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Application.Interfaces.Roles;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Roles;

public class RoleRepository : IRoleRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RoleRepository> _logger;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public RoleRepository(
        NabdCareDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<RoleRepository> logger,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _logger = logger;
        _cacheInvalidator = cacheInvalidator;
    }

    private bool IsSuperAdmin() => _tenantContext.IsSuperAdmin;
    private Guid? CurrentClinicId => _tenantContext.ClinicId;
    private string? CurrentUserId => _tenantContext.UserId?.ToString();

    // =================================================================
    // 🏗️ SHARED QUERY BUILDER (Private)
    // =================================================================
private IQueryable<Role> BuildQuery(RoleFilterRequestDto filter, Func<IQueryable<Role>, IQueryable<Role>>? abacFilter)
    {
        var query = _dbContext.Roles
            .IgnoreQueryFilters()
            .Include(r => r.Clinic)
            .AsNoTracking();

        // 1. Security & Deletion
        if (filter.IncludeDeleted)
        {
            if (!IsSuperAdmin() && CurrentClinicId.HasValue)
            {
                query = query.Where(r => 
                    r.IsSystemRole || 
                    r.IsTemplate || 
                    r.ClinicId == CurrentClinicId);
            }
        }
        else
        {
            query = query.Where(r => !r.IsDeleted);
        }

        // 2. Explicit Filters (Boolean Flags)
        // Only apply these if they are explicitly set to true/false
        if (filter.ClinicId.HasValue)
            query = query.Where(r => r.ClinicId == filter.ClinicId.Value);

        if (filter.IsSystemRole.HasValue)
            query = query.Where(r => r.IsSystemRole == filter.IsSystemRole.Value);

        if (filter.IsTemplate.HasValue)
            query = query.Where(r => r.IsTemplate == filter.IsTemplate.Value);

        // 3. "Role Origin" Helper (String based for Dropdown)
        if (!string.IsNullOrWhiteSpace(filter.RoleOrigin))
        {
            var origin = filter.RoleOrigin.Trim().ToLower();
            
            if (origin == "system") 
            {
                // SaaS Level Roles
                query = query.Where(r => r.IsSystemRole);
            }
            else if (origin == "template") 
            {
                // Blueprint Roles
                query = query.Where(r => r.IsTemplate);
            }
            else if (origin == "clinic") 
            {
                query = query.Where(r => !r.IsSystemRole);
            }
        }

        // 4. Search
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(searchLower));
        }
        
        // 5. Date Range
        if (filter.FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);

        // 6. ABAC
        if (abacFilter != null)
            query = abacFilter(query);

        return query;
    }

    // =================================================================
    // 📋 QUERY METHODS
    // =================================================================

    public async Task<PaginatedResult<Role>> GetAllPagedAsync(
        RoleFilterRequestDto filter,
        Func<IQueryable<Role>, IQueryable<Role>>? abacFilter = null)
    {
        int limit = filter.Limit <= 0 ? 20 : (filter.Limit > 100 ? 100 : filter.Limit);

        var query = BuildQuery(filter, abacFilter);

        // 📸 Total Count Snapshot
        var totalCount = await query.CountAsync();

        // Cursor Logic
        var (createdAtCursor, idCursor) = DecodeCursor(filter.Cursor);
        if (createdAtCursor.HasValue)
        {
            if (filter.Descending)
                query = query.Where(r => r.CreatedAt < createdAtCursor.Value);
            else
                query = query.Where(r => r.CreatedAt > createdAtCursor.Value);
        }

        // Ordering (Must be stable for cursor)
        query = filter.Descending
            ? query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            : query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id);

        // Fetch
        var roles = await query.Take(limit + 1).ToListAsync();

        bool hasMore = roles.Count > limit;
        if (hasMore) roles.RemoveAt(roles.Count - 1);

        string? nextCursor = hasMore && roles.LastOrDefault() is { } last
            ? EncodeCursor(last.CreatedAt, last.Id)
            : null;

        return new PaginatedResult<Role>
        {
            Items = roles,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync(
        RoleFilterRequestDto filter,
        Func<IQueryable<Role>, IQueryable<Role>>? abacFilter = null)
    {
        var query = BuildQuery(filter, abacFilter);

        // Simple sorting for dropdowns
        return await query
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetSystemRolesAsync()
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => r.IsSystemRole && !r.IsDeleted)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetTemplateRolesAsync()
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => r.IsTemplate && !r.IsDeleted)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Role?> GetRoleByIdAsync(Guid id)
    {
        return await _dbContext.Roles
            .IgnoreQueryFilters()
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
            .Where(r => r.Name == name && r.ClinicId == clinicId && !r.IsDeleted);

        if (excludeRoleId.HasValue)
            query = query.Where(r => r.Id != excludeRoleId.Value);

        return await query.AnyAsync();
    }

    // =================================================================
    // COMMAND METHODS
    // =================================================================

    public async Task<Role> CreateRoleAsync(Role role)
    {
        await _dbContext.Roles.AddAsync(role);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Created role {RoleId} with name {RoleName}", role.Id, role.Name);
        return role;
    }

    public async Task<Role?> UpdateRoleAsync(Role role)
    {
        var existing = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == role.Id);

        if (existing == null) return null;

        if (existing.IsSystemRole && !IsSuperAdmin())
            throw new UnauthorizedAccessException("System roles can only be modified by SuperAdmin");

        if (!IsSuperAdmin() && (existing.ClinicId == null || existing.ClinicId != CurrentClinicId))
            throw new UnauthorizedAccessException("You can modify only roles belonging to your clinic");

        _dbContext.Entry(existing).CurrentValues.SetValues(role);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = CurrentUserId;

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Updated role {RoleId}", role.Id);
        return existing;
    }

    public async Task<bool> SoftDeleteRoleAsync(Guid id)
    {
        var role = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return false;

        if (role.IsSystemRole && !IsSuperAdmin()) return false;
        if (!IsSuperAdmin() && role.ClinicId != CurrentClinicId) return false;

        role.IsDeleted = true;

        _dbContext.Roles.Update(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HardDeleteRoleAsync(Guid id)
    {
        var role = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return false;

        if (role.IsSystemRole && !IsSuperAdmin()) return false;
        if (!IsSuperAdmin() && role.ClinicId != CurrentClinicId) return false;

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<Role?> RestoreRoleAsync(Guid id)
    {
        var role = await _dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return null;

        if (!role.IsDeleted)
            throw new InvalidOperationException("Role is not deleted");

        if (!IsSuperAdmin() && role.ClinicId != CurrentClinicId)
            throw new UnauthorizedAccessException("You can restore only roles belonging to your clinic");

        role.IsDeleted = false;
        role.DeletedAt = null;
        role.DeletedBy = null;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = CurrentUserId;

        _dbContext.Roles.Update(role);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Restored role {RoleId}", id);
        return role;
    }

    // =================================================================
    // PERMISSION MANAGEMENT
    // =================================================================

    public async Task<IEnumerable<Guid>> GetRolePermissionIdsAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
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

            await _cacheInvalidator.InvalidateRoleAsync(roleId);
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

        await _cacheInvalidator.InvalidateRoleAsync(roleId);
        _logger.LogInformation("Assigned permission {PermissionId} to role {RoleId}", permissionId, roleId);
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

        await _cacheInvalidator.InvalidateRoleAsync(roleId);
        _logger.LogInformation("Removed permission {PermissionId} from role {RoleId}", permissionId, roleId);
        return true;
    }

    public async Task<int> BulkAssignPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        var existing = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        var newPermissionIds = permissionIds.Except(existing.Select(x => x.PermissionId)).ToList();

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
        await _cacheInvalidator.InvalidateRoleAsync(roleId);
        _logger.LogInformation("Bulk assigned {Count} permissions to role {RoleId}", newPermissionIds.Count, roleId);
        return newPermissionIds.Count;
    }

    public async Task<bool> SyncRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
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
        await _cacheInvalidator.InvalidateRoleAsync(roleId);
        _logger.LogInformation("Synced {Count} permissions for role {RoleId}", permissionIds.Count(), roleId);
        return true;
    }

    // =================================================================
    // 🛡️ HELPER METHODS
    // =================================================================
    private static (DateTime? createdAt, Guid? id) DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return (null, null);
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict == null) return (null, null);

            DateTime? createdAt = dict.TryGetValue("c", out var cStr) && DateTime.TryParse(cStr, out var cVal) ? cVal : null;
            Guid? id = dict.TryGetValue("i", out var iStr) && Guid.TryParse(iStr, out var iVal) ? iVal : null;
            return (createdAt, id);
        }
        catch { return (null, null); }
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var payload = new Dictionary<string, string>
        {
            ["c"] = createdAt.ToString("O"),
            ["i"] = id.ToString()
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    }
}