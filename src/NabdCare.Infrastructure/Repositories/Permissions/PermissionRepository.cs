using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Permissions;

/// <summary>
/// Repository for managing AppPermissions with cursor-based pagination,
/// and for handling role and user permission assignments.
/// </summary>
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

    // ============================================
    // PAGINATED QUERIES
    // ============================================

    public async Task<PaginatedResult<AppPermission>> GetAllPagedAsync(PaginationRequestDto pagination)
    {
        IQueryable<AppPermission> query = _dbContext.AppPermissions.AsNoTracking();

        // Filtering
        if (!string.IsNullOrWhiteSpace(pagination.Filter))
        {
            var filter = pagination.Filter.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(filter) ||
                (p.Description != null && p.Description.ToLower().Contains(filter)));
        }

        // Sorting
        if (!string.IsNullOrEmpty(pagination.SortBy))
        {
            query = pagination.SortBy.ToLower() switch
            {
                "name" => pagination.Descending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                "createdat" => pagination.Descending
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

        return await ApplyPaginationAsync(query, pagination);
    }

    private static async Task<PaginatedResult<AppPermission>> ApplyPaginationAsync(
        IQueryable<AppPermission> query,
        PaginationRequestDto pagination)
    {
        var totalCount = await query.CountAsync();

        if (pagination.Limit <= 0)
            pagination.Limit = 20;
        if (pagination.Limit > 100)
            pagination.Limit = 100;

        // Cursor-based logic
        if (!string.IsNullOrEmpty(pagination.Cursor))
        {
            if (Guid.TryParse(pagination.Cursor, out var cursorId))
            {
                query = query.Where(p => p.Id.CompareTo(cursorId) < 0);
            }
        }

        var items = await query
            .Take(pagination.Limit + 1)
            .ToListAsync();

        var hasMore = items.Count > pagination.Limit;
        var nextCursor = hasMore ? items.Last().Id.ToString() : null;

        if (hasMore)
            items.RemoveAt(items.Count - 1);

        return new PaginatedResult<AppPermission>
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    // ============================================
    // BASIC CRUD
    // ============================================

    public async Task<IEnumerable<AppPermission>> GetAllPermissionsAsync()
        => await _dbContext.AppPermissions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<AppPermission?> GetPermissionByIdAsync(Guid permissionId)
        => await _dbContext.AppPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId);

    public async Task<AppPermission> CreatePermissionAsync(AppPermission appPermission)
    {
        appPermission.Id = Guid.NewGuid();
        appPermission.CreatedAt = DateTime.UtcNow;
        appPermission.CreatedBy = _tenant.UserId?.ToString();

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
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _tenant.UserId?.ToString();

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

    // ============================================
    // SEARCH
    // ============================================

    public async Task<IEnumerable<AppPermission>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<AppPermission>();

        var q = query.ToLower();
        return await _dbContext.AppPermissions
            .AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(q) ||
                        (p.Description != null && p.Description.ToLower().Contains(q)))
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync();
    }

    // ============================================
    // ROLE PERMISSIONS
    // ============================================

    public async Task<IEnumerable<AppPermission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .Include(rp => rp.AppPermission)
            .Where(rp => rp.RoleId == roleId && !rp.IsDeleted)
            .Select(rp => rp.AppPermission)
            .OrderBy(p => p.Name)
            .AsNoTracking()
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

        if (await _dbContext.RolePermissions.AnyAsync(rp =>
                rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted))
            return false;

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenant.UserId?.ToString()
        };

        await _dbContext.RolePermissions.AddAsync(rolePermission);
        await _dbContext.SaveChangesAsync();
        await _cacheInvalidator.InvalidateRoleAsync(roleId);

        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var existing = await _dbContext.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && !rp.IsDeleted);

        if (existing == null)
            return false;

        _dbContext.RolePermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();
        await _cacheInvalidator.InvalidateRoleAsync(roleId);

        return true;
    }

    // ============================================
    // USER PERMISSIONS
    // ============================================

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
            .OrderBy(p => p.Name)
            .AsNoTracking()
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

        if (await _dbContext.UserPermissions.AnyAsync(up =>
                up.UserId == userId && up.PermissionId == permissionId && !up.IsDeleted))
            return false;

        var userPermission = new UserPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenant.UserId?.ToString()
        };

        await _dbContext.UserPermissions.AddAsync(userPermission);
        await _dbContext.SaveChangesAsync();
        await _cacheInvalidator.InvalidateUserAsync(userId);

        return true;
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var existing = await _dbContext.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId && !up.IsDeleted);

        if (existing == null)
            return false;

        _dbContext.UserPermissions.Remove(existing);
        await _dbContext.SaveChangesAsync();
        await _cacheInvalidator.InvalidateUserAsync(userId);

        return true;
    }
}