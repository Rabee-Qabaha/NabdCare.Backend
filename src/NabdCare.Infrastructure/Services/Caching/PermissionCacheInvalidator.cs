using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Interfaces.Permissions;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Services.Caching;

public class PermissionCacheInvalidator : IPermissionCacheInvalidator
{
    private readonly IMemoryCache _cache;
    private readonly NabdCareDbContext _db;
    private readonly ILogger<PermissionCacheInvalidator> _logger;

    public PermissionCacheInvalidator(IMemoryCache cache, NabdCareDbContext db, ILogger<PermissionCacheInvalidator> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public Task InvalidateUserAsync(Guid userId)
    {
        var cacheKey = $"permissions:{userId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("🧹 Cache invalidated for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public async Task InvalidateRoleAsync(Guid roleId)
    {
        var userIds = await _db.Users
            .Where(u => u.RoleId == roleId && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var uid in userIds)
        {
            _cache.Remove($"permissions:{uid}");
        }

        _logger.LogInformation("🧹 Cache invalidated for role {RoleId} affecting {Count} users", roleId, userIds.Count);
    }
}