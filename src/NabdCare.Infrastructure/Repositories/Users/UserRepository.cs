using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Users;

/// <summary>
/// Repository for user data with ABAC-ready query filtering support.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly NabdCareDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        NabdCareDbContext dbContext,
        ILogger<UserRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region QUERY METHODS

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
    }

    public async Task<User?> GetByIdRawAsync(Guid id)
    {
        return await _dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    /// <summary>
    /// Cursor-based pagination for all users (SuperAdmin).
    /// Optional ABAC filter allows contextual visibility (e.g., clinic restriction).
    /// </summary>
    public async Task<PaginatedResult<User>> GetAllPagedAsync(
        int limit,
        string? cursor,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null)
    {
        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var (createdAtCursor, idCursor) = DecodeCursor(cursor);

        IQueryable<User> query = _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted)
            .AsNoTracking();

        // 🔒 Apply ABAC filter if provided (e.g., restrict to clinic)
        if (abacFilter != null)
            query = abacFilter(query);

        if (createdAtCursor.HasValue && idCursor.HasValue)
        {
            query = query.Where(u =>
                (u.CreatedAt > createdAtCursor.Value) ||
                (u.CreatedAt == createdAtCursor.Value && u.Id.CompareTo(idCursor.Value) > 0));
        }

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = users.Count > limit;
        if (hasMore) users.RemoveAt(users.Count - 1);

        string? nextCursor = hasMore && users.LastOrDefault() is { } last
            ? EncodeCursor(last.CreatedAt, last.Id)
            : null;

        var totalCount = await query.CountAsync();

        return new PaginatedResult<User>
        {
            Items = users,
            HasMore = hasMore,
            NextCursor = nextCursor,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Cursor-based pagination filtered by clinic (still supports optional ABAC filter).
    /// </summary>
    public async Task<PaginatedResult<User>> GetByClinicIdPagedAsync(
        Guid clinicId,
        int limit,
        string? cursor,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));

        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var (createdAtCursor, idCursor) = DecodeCursor(cursor);

        IQueryable<User> query = _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Where(u => u.ClinicId == clinicId && !u.IsDeleted)
            .AsNoTracking();

        if (abacFilter != null)
            query = abacFilter(query);

        if (createdAtCursor.HasValue && idCursor.HasValue)
        {
            query = query.Where(u =>
                (u.CreatedAt > createdAtCursor.Value) ||
                (u.CreatedAt == createdAtCursor.Value && u.Id.CompareTo(idCursor.Value) > 0));
        }

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = users.Count > limit;
        if (hasMore) users.RemoveAt(users.Count - 1);

        string? nextCursor = hasMore && users.LastOrDefault() is { } last
            ? EncodeCursor(last.CreatedAt, last.Id)
            : null;

        var totalCount = await query.CountAsync();

        return new PaginatedResult<User>
        {
            Items = users,
            HasMore = hasMore,
            NextCursor = nextCursor,
            TotalCount = totalCount
        };
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    public async Task<IEnumerable<User>> GetUsersByRoleIdAsync(Guid roleId)
    {
        return await _dbContext.Users
            .Where(u => u.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && !u.IsDeleted);
    }

    #endregion

    #region COMMAND METHODS
    public async Task<User> CreateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        user.Email = user.Email.Trim().ToLower();
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created", user.Id);
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (!string.IsNullOrWhiteSpace(user.Email))
            user.Email = user.Email.Trim().ToLower();

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated", user.Id);
        return user;
    }

    public async Task<bool> SoftDeleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} soft deleted", userId);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("User {UserId} permanently deleted", userId);
        return true;
    }
    #endregion

    #region PRIVATE HELPERS
    private static (DateTime? createdAt, Guid? id) DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return (null, null);
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict == null) return (null, null);

            DateTime? createdAt = dict.TryGetValue("createdAt", out var caStr) &&
                                  DateTime.TryParse(caStr, out var parsedCa)
                ? parsedCa
                : null;

            Guid? id = dict.TryGetValue("id", out var idStr) &&
                        Guid.TryParse(idStr, out var parsedId)
                ? parsedId
                : null;

            return (createdAt, id);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var payload = new Dictionary<string, string>
        {
            ["createdAt"] = createdAt.ToString("O"),
            ["id"] = id.ToString()
        };
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
    #endregion
}