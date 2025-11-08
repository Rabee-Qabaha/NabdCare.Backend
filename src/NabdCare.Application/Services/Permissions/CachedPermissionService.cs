using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Application.Interfaces.Permissions;

namespace NabdCare.Application.Services.Permissions;

public class CachedPermissionService : IPermissionService
{
    private readonly PermissionService _permissionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedPermissionService> _logger;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RoleTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UserTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(5);

    public CachedPermissionService(
        PermissionService permissionService,
        IMemoryCache cache,
        ILogger<CachedPermissionService> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string Key(Guid userId, Guid roleId) => $"perm:eff:{userId}:{roleId}";
    private static string KeyRole(Guid roleId) => $"perm:role:{roleId}";
    private static string KeyUser(Guid userId) => $"perm:user:{userId}";
    private static string VersionKey(Guid userId, Guid roleId) => $"perm:ver:{userId}:{roleId}";

    public Task<PaginatedResult<PermissionResponseDto>> GetAllPagedAsync(PaginationRequestDto pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        _logger.LogDebug("Retrieving all paginated permissions (Limit={Limit})", pagination.Limit);

        return _permissionService.GetAllPagedAsync(pagination);
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetUserEffectivePermissionsAsync(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        var cacheKey = Key(userId, roleId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) &&
            cached is { })
        {
            var cachedList = cached.ToList();
            
            if (cachedList.Any())
            {
                _logger.LogDebug("Cache hit for effective permissions (User {UserId}, Role {RoleId})", userId, roleId);
                return cachedList;
            }
        }

        _logger.LogDebug("Cache miss for effective permissions (User {UserId}, Role {RoleId}). Fetching from service.", userId, roleId);

        var fresh = (await _permissionService.GetUserEffectivePermissionsAsync(userId, roleId)).ToList();

        if (!fresh.Any())
        {
            _logger.LogWarning("No permissions found for User {UserId}, Role {RoleId}. Skipping cache.", userId, roleId);
            return fresh;
        }

        _cache.Set(cacheKey, fresh, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        });

        UpdateVersion(userId, roleId);

        _logger.LogDebug("Cached {Count} effective permissions for User {UserId}, Role {RoleId}",
            fresh.Count, userId, roleId);
        return fresh;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, Guid roleId, string permissionName)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        if (string.IsNullOrWhiteSpace(permissionName))
        {
            _logger.LogWarning("Empty permission name provided. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission name cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionName));
        }

        _logger.LogDebug("Checking if user {UserId} has permission {PermissionName}", userId, permissionName);

        var eff = await GetUserEffectivePermissionsAsync(userId, roleId);
        var hasPermission = eff.Any(p => p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            _logger.LogWarning("User {UserId} does not have permission {PermissionName}. Error code: {ErrorCode}",
                userId, permissionName, ErrorCodes.INSUFFICIENT_PERMISSIONS);
        }
        else
        {
            _logger.LogDebug("User {UserId} has permission {PermissionName}", userId, permissionName);
        }

        return hasPermission;
    }

    public void InvalidateUser(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        _cache.Remove(Key(userId, roleId));
        _cache.Remove(VersionKey(userId, roleId));

        _logger.LogInformation("Invalidated effective permission cache for user {UserId}, role {RoleId}",
            userId, roleId);
    }

    public void InvalidateRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        _cache.Remove(KeyRole(roleId));

        _logger.LogInformation("Invalidated role permission cache for role {RoleId}", roleId);
    }

    public void InvalidateUserPermissions(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        _cache.Remove(KeyUser(userId));

        _logger.LogInformation("Invalidated user permission cache for user {UserId}", userId);
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByRoleAsync(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        var cacheKey = KeyRole(roleId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for role {RoleId} permissions", roleId);
            return cached;
        }

        _logger.LogDebug("Cache miss for role {RoleId} permissions. Fetching from service.", roleId);

        var fresh = (await _permissionService.GetPermissionsByRoleAsync(roleId)).ToList();

        _cache.Set(cacheKey, fresh, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RoleTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        });

        _logger.LogDebug("Cached {Count} permissions for role {RoleId}", fresh.Count, roleId);
        return fresh;
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsByUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        var cacheKey = KeyUser(userId);

        if (_cache.TryGetValue(cacheKey, out IEnumerable<PermissionResponseDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for user {UserId} permissions", userId);
            return cached;
        }

        _logger.LogDebug("Cache miss for user {UserId} permissions. Fetching from service.", userId);

        var fresh = (await _permissionService.GetPermissionsByUserAsync(userId)).ToList();

        _cache.Set(cacheKey, fresh, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = UserTtl,
            SlidingExpiration = SlidingTtl,
            Size = Math.Max(1, fresh.Count)
        });

        _logger.LogDebug("Cached {Count} user permissions for user {UserId}", fresh.Count, userId);
        return fresh;
    }

    private void UpdateVersion(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        var version = DateTime.UtcNow.ToString("O");
        _cache.Set(VersionKey(userId, roleId), version, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            Size = 1
        });

        _logger.LogInformation("Updated permission version {Version} for user {UserId}, role {RoleId}",
            version, userId, roleId);
    }

    public string? GetVersion(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        _cache.TryGetValue(VersionKey(userId, roleId), out string? version);

        _logger.LogDebug("Retrieved permission version {Version} for user {UserId}, role {RoleId}",
            version ?? "null", userId, roleId);

        return version;
    }

    public Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
    {
        _logger.LogDebug("Retrieving all permissions");
        return _permissionService.GetAllPermissionsAsync();
    }

    public Task<PermissionResponseDto?> GetPermissionByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogDebug("Retrieving permission {PermissionId}", id);
        return _permissionService.GetPermissionByIdAsync(id);
    }

    public Task<PermissionResponseDto> CreatePermissionAsync(CreatePermissionDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.LogWarning("Attempt to create permission with empty name. Error code: {ErrorCode}",
                ErrorCodes.INVALID_ARGUMENT);
            throw new ArgumentException($"Permission name cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(dto.Name));
        }

        _logger.LogInformation("Creating permission {PermissionName}", dto.Name);
        return _permissionService.CreatePermissionAsync(dto);
    }

    public Task<PermissionResponseDto?> UpdatePermissionAsync(Guid id, UpdatePermissionDto dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        _logger.LogInformation("Updating permission {PermissionId}", id);
        return _permissionService.UpdatePermissionAsync(id, dto);
    }

    public Task<bool> DeletePermissionAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(id));

        _logger.LogInformation("Deleting permission {PermissionId}", id);
        return _permissionService.DeletePermissionAsync(id);
    }

    public async Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);

        var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId);
        
        if (result)
        {
            InvalidateRole(roleId);

            var affectedUsers = (await _permissionService.GetUsersByRoleAsync(roleId)).ToList();
            foreach (var u in affectedUsers)
            {
                UpdateVersion(u.UserId, roleId);
            }

            _logger.LogInformation("Successfully assigned permission {PermissionId} to role {RoleId}. Invalidated {Count} user caches",
                permissionId, roleId, affectedUsers.Count);
        }

        return result;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty)
            throw new ArgumentException($"Role ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(roleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Removing permission {PermissionId} from role {RoleId}", permissionId, roleId);

        var result = await _permissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
        
        if (result)
        {
            InvalidateRole(roleId);

            var affectedUsers = (await _permissionService.GetUsersByRoleAsync(roleId)).ToList();
            foreach (var u in affectedUsers)
            {
                UpdateVersion(u.UserId, roleId);
            }

            _logger.LogInformation("Successfully removed permission {PermissionId} from role {RoleId}. Invalidated {Count} user caches",
                permissionId, roleId, affectedUsers.Count);
        }

        return result;
    }

    public async Task<bool> AssignPermissionToUserAsync(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Assigning permission {PermissionId} to user {UserId}", permissionId, userId);

        var result = await _permissionService.AssignPermissionToUserAsync(userId, permissionId);
        
        if (result)
        {
            InvalidateUserPermissions(userId);

            var userInfo = await _permissionService.GetUserForAuthorizationAsync(userId);
            if (userInfo.HasValue)
            {
                UpdateVersion(userId, userInfo.Value.RoleId);
            }

            _logger.LogInformation("Successfully assigned permission {PermissionId} to user {UserId}", permissionId, userId);
        }

        return result;
    }

    public async Task<bool> RemovePermissionFromUserAsync(Guid userId, Guid permissionId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException($"Permission ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(permissionId));

        _logger.LogInformation("Removing permission {PermissionId} from user {UserId}", permissionId, userId);

        var result = await _permissionService.RemovePermissionFromUserAsync(userId, permissionId);
        
        if (result)
        {
            InvalidateUserPermissions(userId);

            var userInfo = await _permissionService.GetUserForAuthorizationAsync(userId);
            if (userInfo.HasValue)
            {
                UpdateVersion(userId, userInfo.Value.RoleId);
            }

            _logger.LogInformation("Successfully removed permission {PermissionId} from user {UserId}", permissionId, userId);
        }

        return result;
    }

    public Task<(Guid RoleId, Guid? ClinicId)?> GetUserForAuthorizationAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Error code: {ErrorCodes.INVALID_ARGUMENT}", nameof(userId));

        _logger.LogDebug("Retrieving authorization info for user {UserId}", userId);
        return _permissionService.GetUserForAuthorizationAsync(userId);
    }
}