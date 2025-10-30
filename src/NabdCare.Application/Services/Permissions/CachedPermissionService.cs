using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Permissions;

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
    // Effective Permissions (User + Role)
    // ===============================================================
    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        var cacheKey = Key(userId, roleId);

        // Try to read from cache
        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached))
        {
            // ✅ only return if not null AND not empty
            if (cached != null && cached.Any())
            {
                _logger.LogDebug("✅ Cache hit for user {UserId}, role {RoleId} ({Count} permissions)", userId, roleId, cached.Count());
                return cached;
            }
            else
            {
                _logger.LogDebug("⚠️ Cache hit but empty for user {UserId}, role {RoleId} — refreshing from source", userId, roleId);
            }
        }

        // Load fresh data
        var fresh = (await _inner.GetUserEffectivePermissionsAsync(userId, roleId)).ToList();

        // ✅ Don’t cache empty results
        if (fresh.Count == 0)
        {
            _logger.LogWarning("🚫 No permissions found for user {UserId}, role {RoleId}. Skipping cache.", userId, roleId);
            return fresh;
        }

        // Add to cache
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        };

        _cache.Set(cacheKey, fresh, options);
        _logger.LogDebug("🧠 Cached {Count} permissions for user {UserId}, role {RoleId}", fresh.Count, userId, roleId);

        return fresh;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName)
    {
        var eff = await GetUserEffectivePermissionsAsync(userId, roleId);
        return eff.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    // ===============================================================
    // Cache Invalidation Helpers
    // ===============================================================
    public void InvalidateUser(Guid userId, Guid roleId)
    {
        _cache.Remove(Key(userId, roleId));
        _logger.LogDebug("🧹 Invalidated permission cache for user {UserId}, role {RoleId}", userId, roleId);
    }

    public void InvalidateRole(Guid roleId)
    {
        _cache.Remove(KeyRole(roleId));
        _logger.LogDebug("🧹 Invalidated role permission cache for role {RoleId}", roleId);
    }

    public void InvalidateUserPermissions(Guid userId)
    {
        _cache.Remove(KeyUser(userId));
        _logger.LogDebug("🧹 Invalidated user-specific permission cache for user {UserId}", userId);
    }

    // ===============================================================
    // Role-Level Caching
    // ===============================================================
    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId)
    {
        var cacheKey = KeyRole(roleId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null)
            return cached;

        var fresh = (await _inner.GetPermissionsByRoleAsync(roleId)).ToList();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RoleTtl,
            SlidingExpiration = SlidingTtl,
            Size = fresh.Count
        };

        _cache.Set(cacheKey, fresh, options);
        _logger.LogDebug("🧠 Cached role permissions for role {RoleId}", roleId);

        return fresh;
    }

    // ===============================================================
    // User-Level Caching
    // ===============================================================
    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId)
    {
        var cacheKey = KeyUser(userId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null)
            return cached;

        var fresh = (await _inner.GetPermissionsByUserAsync(userId)).ToList();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = UserTtl,
            SlidingExpiration = SlidingTtl,
            Size = fresh.Count
        };

        _cache.Set(cacheKey, fresh, options);
        _logger.LogDebug("🧠 Cached user-specific permissions for user {UserId}", userId);

        return fresh;
    }

    // ===============================================================
    // Pass-through CRUD operations
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

    public Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
        => _inner.AssignPermissionToRoleAsync(roleId, permissionId);

    public Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
        => _inner.RemovePermissionFromRoleAsync(roleId, permissionId);

    public Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
        => _inner.AssignPermissionToUserAsync(userId, permissionId);

    public Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
        => _inner.RemovePermissionFromUserAsync(userId, permissionId);

    public Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName, CancellationToken _ = default)
        => _inner.UserHasPermissionAsync(userId, roleId, permissionName);

    public Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
        => _inner.GetUserForAuthorizationAsync(userId);
}