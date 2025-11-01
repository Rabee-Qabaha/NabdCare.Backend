using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Permissions;

/// <summary>
/// Wraps PermissionService with in-memory caching for performance.
/// Caches user, role, and effective permissions with version tracking and invalidation.
/// </summary>
public class CachedPermissionService : IPermissionService
{
    private readonly PermissionService   _permissionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedPermissionService> _logger;

    // Default cache durations
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RoleTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UserTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(5);

    public CachedPermissionService(
        PermissionService permissionService,
        IMemoryCache cache,
        ILogger<CachedPermissionService> logger)
    {
        _permissionService = permissionService;
        _cache = cache;
        _logger = logger;
    }

    // ===============================================================
    // CACHE KEYS
    // ===============================================================

    private static string Key(Guid userId, Guid roleId) => $"perm:eff:{userId}:{roleId}";
    private static string KeyRole(Guid roleId) => $"perm:role:{roleId}";
    private static string KeyUser(Guid userId) => $"perm:user:{userId}";
    private static string VersionKey(Guid userId, Guid roleId) => $"perm:ver:{userId}:{roleId}";

    // ===============================================================
    // PAGINATED PERMISSIONS
    // ===============================================================

    public Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
        => _permissionService.GetAllPagedAsync(pagination);

    // ===============================================================
    // EFFECTIVE PERMISSIONS (User + Role)
    // ===============================================================

    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        var cacheKey = Key(userId, roleId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) &&
            cached is { } && cached.Any())
        {
            _logger.LogDebug("✅ Cache hit for effective permissions (User {UserId}, Role {RoleId})", userId, roleId);
            return cached;
        }

        var fresh = (await _permissionService.GetUserEffectivePermissionsAsync(userId, roleId)).ToList();

        if (!fresh.Any())
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

        // ✅ Update permissions version whenever we fetch fresh data
        UpdateVersion(userId, roleId);

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
        _cache.Remove(VersionKey(userId, roleId));
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

        var fresh = (await _permissionService.GetPermissionsByRoleAsync(roleId)).ToList();

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

        var fresh = (await _permissionService.GetPermissionsByUserAsync(userId)).ToList();

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
    // VERSION TRACKING
    // ===============================================================

    private void UpdateVersion(Guid userId, Guid roleId)
    {
        var version = DateTime.UtcNow.ToString("O");
        _cache.Set(VersionKey(userId, roleId), version, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            Size = 1
        });

        _logger.LogInformation("🔁 Updated permission version {Version} (User {UserId}, Role {RoleId})", version, userId, roleId);
    }

    public string? GetVersion(Guid userId, Guid roleId)
    {
        _cache.TryGetValue(VersionKey(userId, roleId), out string? version);
        return version;
    }

    // ===============================================================
    // PASS-THROUGH CRUD OPERATIONS
    // ===============================================================

    public Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
        => _permissionService.GetAllPermissionsAsync();

    public Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
        => _permissionService.GetPermissionByIdAsync(id);

    public Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
        => _permissionService.CreatePermissionAsync(dto);

    public Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
        => _permissionService.UpdatePermissionAsync(id, dto);

    public Task<bool> DeletePermissionAsync(Guid id)
        => _permissionService.DeletePermissionAsync(id);

    // ===============================================================
    // ROLE / USER ASSIGNMENTS (with version updates)
    // ===============================================================

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId);
        InvalidateRole(roleId);

        var affectedUsers = await _permissionService.GetUsersByRoleAsync(roleId);
        foreach (var u in affectedUsers)
            UpdateVersion(u.UserId, roleId);

        return result;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var result = await _permissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
        InvalidateRole(roleId);

        var affectedUsers = await _permissionService.GetUsersByRoleAsync(roleId);
        foreach (var u in affectedUsers)
            UpdateVersion(u.UserId, roleId);

        return result;
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        var result = await _permissionService.AssignPermissionToUserAsync(userId, permissionId);
        InvalidateUserPermissions(userId);

        var userInfo = await _permissionService.GetUserForAuthorizationAsync(userId);
        if (userInfo.HasValue)
            UpdateVersion(userId, userInfo.Value.RoleId);

        return result;
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        var result = await _permissionService.RemovePermissionFromUserAsync(userId, permissionId);
        InvalidateUserPermissions(userId);

        var userInfo = await _permissionService.GetUserForAuthorizationAsync(userId);
        if (userInfo.HasValue)
            UpdateVersion(userId, userInfo.Value.RoleId);

        return result;
    }

    // ===============================================================
    // AUTHORIZATION HELPERS
    // ===============================================================

    public Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
        => _permissionService.GetUserForAuthorizationAsync(userId);
}