using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Permissions;

/// <summary>
/// Wraps PermissionService with in-memory caching for performance.
/// Caches user, role, and effective permissions with time-based invalidation.
/// </summary>
public class CachedPermissionService : IPermissionService
{
    private readonly PermissionService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedPermissionService> _logger;

    // Default cache durations
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RoleTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UserTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(5);

    public CachedPermissionService(
        PermissionService inner,
        IMemoryCache cache,
        ILogger<CachedPermissionService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    private static string Key(Guid userId, Guid roleId) => $"perm:eff:{userId}:{roleId}";
    private static string KeyRole(Guid roleId) => $"perm:role:{roleId}";
    private static string KeyUser(Guid userId) => $"perm:user:{userId}";

    // ===============================================================
    // PAGINATED PERMISSIONS
    // ===============================================================

    public Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
        => _inner.GetAllPagedAsync(pagination);

    // ===============================================================
    // EFFECTIVE PERMISSIONS (User + Role)
    // ===============================================================

    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        var cacheKey = Key(userId, roleId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null && cached.Any())
        {
            _logger.LogDebug("✅ Cache hit for effective permissions (User {UserId}, Role {RoleId})", userId, roleId);
            return cached;
        }

        var fresh = (await _inner.GetUserEffectivePermissionsAsync(userId, roleId)).ToList();

        if (fresh.Count == 0)
        {
            _logger.LogWarning("⚠️ No permissions found for User {UserId}, Role {RoleId}. Skipping cache.", userId, roleId);
            return fresh;
        }

        _cache.Set(cacheKey, fresh, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        });

        _logger.LogDebug("🧠 Cached {Count} effective permissions for User {UserId}, Role {RoleId}", fresh.Count, userId, roleId);
        return fresh;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName)
    {
        var eff = await GetUserEffectivePermissionsAsync(userId, roleId);
        return eff.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    // ===============================================================
    // CACHE INVALIDATION
    // ===============================================================

    public void InvalidateUser(Guid userId, Guid roleId)
    {
        _cache.Remove(Key(userId, roleId));
        _logger.LogDebug("🧹 Invalidated effective permission cache (User {UserId}, Role {RoleId})", userId, roleId);
    }

    public void InvalidateRole(Guid roleId)
    {
        _cache.Remove(KeyRole(roleId));
        _logger.LogDebug("🧹 Invalidated role permission cache (Role {RoleId})", roleId);
    }

    public void InvalidateUserPermissions(Guid userId)
    {
        _cache.Remove(KeyUser(userId));
        _logger.LogDebug("🧹 Invalidated user permission cache (User {UserId})", userId);
    }

    // ===============================================================
    // ROLE-LEVEL CACHING
    // ===============================================================

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId)
    {
        var cacheKey = KeyRole(roleId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null)
        {
            _logger.LogDebug("✅ Cache hit for role {RoleId} permissions", roleId);
            return cached;
        }

        var fresh = (await _inner.GetPermissionsByRoleAsync(roleId)).ToList();

        _cache.Set(cacheKey, fresh, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RoleTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        });

        _logger.LogDebug("🧠 Cached {Count} permissions for role {RoleId}", fresh.Count, roleId);
        return fresh;
    }

    // ===============================================================
    // USER-LEVEL CACHING
    // ===============================================================

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId)
    {
        var cacheKey = KeyUser(userId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null)
        {
            _logger.LogDebug("✅ Cache hit for user {UserId} permissions", userId);
            return cached;
        }

        var fresh = (await _inner.GetPermissionsByUserAsync(userId)).ToList();

        _cache.Set(cacheKey, fresh, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = UserTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        });

        _logger.LogDebug("🧠 Cached {Count} user permissions for user {UserId}", fresh.Count, userId);
        return fresh;
    }

    // ===============================================================
    // PASS-THROUGH CRUD OPERATIONS
    // ===============================================================

    public Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
        => _inner.GetAllPermissionsAsync();

    public Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
        => _inner.GetPermissionByIdAsync(id);

    public Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
        => _inner.CreatePermissionAsync(dto);

    public Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
        => _inner.UpdatePermissionAsync(id, dto);

    public Task<bool> DeletePermissionAsync(Guid id)
        => _inner.DeletePermissionAsync(id);

    // ===============================================================
    // ROLE / USER ASSIGNMENTS
    // ===============================================================

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var result = await _inner.AssignPermissionToRoleAsync(roleId, permissionId);
        InvalidateRole(roleId);
        return result;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var result = await _inner.RemovePermissionFromRoleAsync(roleId, permissionId);
        InvalidateRole(roleId);
        return result;
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        var result = await _inner.AssignPermissionToUserAsync(userId, permissionId);
        InvalidateUserPermissions(userId);
        return result;
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var result = await _inner.RemovePermissionFromUserAsync(userId, permissionId);
        InvalidateUserPermissions(userId);
        return result;
    }

    // ===============================================================
    // AUTHORIZATION HELPERS
    // ===============================================================

    public Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
        => _inner.GetUserForAuthorizationAsync(userId);
}