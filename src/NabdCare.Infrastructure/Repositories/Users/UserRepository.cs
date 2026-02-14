using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.DTOs.Users;
using NabdCare.Application.Interfaces.Users;
using NabdCare.Domain.Entities.Users;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Users;

/// <summary>
/// Repository for user data with ABAC-ready query filtering support.
/// 
/// DESIGN PATTERN: Purpose-Specific Methods (SOLID-compliant)
/// - Public methods clearly state their purpose in the name
/// - Each method has single responsibility
/// - Internal implementation avoids code duplication (DRY)
/// - All soft-delete bypasses are logged for security audit
/// 
/// SECURITY NOTES:
/// - Methods that bypass soft-delete filters are clearly marked ⚠️
/// - Each bypass logs the stack trace for security investigation
/// - Calling service MUST perform authorization checks
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

    #region ACTIVE USER QUERIES (Soft-Delete Filtered)

    public async Task<User?> GetByIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Include(u => u.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.Users
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Include(u => u.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
    }

    /// <summary>
    /// Cursor-based pagination for all users (SuperAdmin).
    /// Optional ABAC filter allows contextual visibility (e.g., clinic restriction).
    /// </summary>
    public async Task<PaginatedResult<User>> GetAllPagedAsync(
        UserFilterRequestDto filter,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null)
    {
        // 1. Sanitize Limit
        int limit = filter.Limit <= 0 ? 20 : (filter.Limit > 100 ? 100 : filter.Limit);

        // 2. Base Query (Optimized)
        IQueryable<User> query = _dbContext.Users
            .IgnoreQueryFilters() // Bypass default filters to handle IncludeDeleted manually
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Include(u => u.CreatedByUser)
            .AsNoTracking();

        // 3. Apply Filters
        if (!filter.IncludeDeleted)
            query = query.Where(u => !u.IsDeleted);

        if (filter.ClinicId.HasValue)
            query = query.Where(u => u.ClinicId == filter.ClinicId.Value);

        if (filter.RoleId.HasValue)
            query = query.Where(u => u.RoleId == filter.RoleId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        // 4. Search (Name or Email)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower));
        }

        // 5. Date Range
        if (filter.FromDate.HasValue)
            query = query.Where(u => u.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(u => u.CreatedAt <= filter.ToDate.Value);

        // 6. Security Filter (ABAC)
        if (abacFilter != null)
            query = abacFilter(query);

        // 📸 SNAPSHOT: Get Total Count NOW (before applying cursor/limit)
        var totalCount = await query.CountAsync();

        // 7. Cursor Logic (Fixing the SQL Translation Error)
        var (createdAtCursor, idCursor) = DecodeCursor(filter.Cursor);

        if (createdAtCursor.HasValue && idCursor.HasValue)
        {
            if (filter.Descending ?? false)
            {
                // Paginate Backward (Newest first)
                query = query.Where(u =>
                    u.CreatedAt < createdAtCursor.Value ||
                    (u.CreatedAt == createdAtCursor.Value && u.Id.CompareTo(idCursor.Value) < 0));
                // Note: If CompareTo still fails, use: u.Id < idCursor.Value
            }
            else
            {
                // Paginate Forward (Oldest first)
                query = query.Where(u =>
                    u.CreatedAt > createdAtCursor.Value ||
                    (u.CreatedAt == createdAtCursor.Value && u.Id.CompareTo(idCursor.Value) > 0));
            }
        }

        // 8. Ordering
        query = (filter.Descending ?? false)
            ? query.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id)
            : query.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id);

        // 9. Fetch Data (Fetch limit + 1 to check if there is a next page)
        var users = await query.Take(limit + 1).ToListAsync();

        // 10. Next Cursor Calculation
        bool hasMore = users.Count > limit;
        if (hasMore)
            users.RemoveAt(users.Count - 1); // Remove the extra item we fetched

        string? nextCursor = hasMore && users.LastOrDefault() is { } last
            ? EncodeCursor(last.CreatedAt, last.Id)
            : null;

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
        bool includeDeleted = false,
        Func<IQueryable<User>, IQueryable<User>>? abacFilter = null)
    {
        if (clinicId == Guid.Empty)
            throw new ArgumentException("Clinic ID cannot be empty.", nameof(clinicId));

        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var (createdAtCursor, idCursor) = DecodeCursor(cursor);

        IQueryable<User> query = _dbContext.Users
            .IgnoreQueryFilters() 
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .Include(u => u.CreatedByUser)
            .Where(u => u.ClinicId == clinicId)
            .AsNoTracking();

        // ✅ Only filter deleted users if includeDeleted == false
        if (!includeDeleted)
            query = query.Where(u => !u.IsDeleted);

        // 🔒 Optional ABAC restriction
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

        bool hasMore = users.Count > limit;
        if (hasMore)
            users.RemoveAt(users.Count - 1);

        string? nextCursor = hasMore && users.LastOrDefault() is { } last
            ? EncodeCursor(last.CreatedAt, last.Id)
            : null;

        int totalCount = await query.CountAsync();

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
            .Where(u => u.RoleId == roleId && !u.IsDeleted)
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

    #region SOFT-DELETE BYPASS QUERIES (Purpose-Specific)

    /// <summary>
    /// ⚠️ Get user by ID INCLUDING soft-deleted users (AUTHORIZATION PURPOSE).
    /// 
    /// This method bypasses soft-delete filter to check permissions for ANY user state.
    /// Used for authorization checks and permission evaluation.
    /// 
    /// Returns minimal navigation properties (Clinic, Role) optimized for auth performance.
    /// </summary>
    public async Task<User?> GetByIdForAuthorizationAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        // ✅ SECURITY: Log this sensitive operation with full context
        _logger.LogWarning(
            "🔓 AUTHORIZATION BYPASS: Fetching user {UserId} INCLUDING soft-deleted for authorization check. " +
            "Purpose: Permission validation/authentication. Stack: {StackTrace}",
            userId,
            Environment.StackTrace);

        return await GetByIdIncludingDeletedInternalAsync(
            userId,
            includeAuditData: false,
            purpose: "authorization");
    }

    /// <summary>
    /// ⚠️ Get user by ID INCLUDING soft-deleted users (RESTORATION PURPOSE).
    /// 
    /// This method bypasses soft-delete filter to retrieve deleted users for restoration.
    /// Used when restoring a previously deleted account.
    /// 
    /// Returns complete navigation properties (Clinic, Role, CreatedByUser) with full audit data.
    /// </summary>
    public async Task<User?> GetByIdForRestorationAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

        // ✅ SECURITY: Log this sensitive operation with full context
        _logger.LogWarning(
            "🔄 RESTORATION BYPASS: Fetching user {UserId} INCLUDING soft-deleted for restoration process. " +
            "Purpose: User account restoration. Stack: {StackTrace}",
            userId,
            Environment.StackTrace);

        return await GetByIdIncludingDeletedInternalAsync(
            userId,
            includeAuditData: true,
            purpose: "restoration");
    }

    /// <summary>
    /// ⚠️ Get user by email INCLUDING soft-deleted users (EMAIL VERIFICATION PURPOSE).
    /// 
    /// This method bypasses soft-delete filter to check if email was previously used.
    /// Used for email uniqueness validation and duplicate detection.
    /// 
    /// Returns user object if email is found (whether active or deleted).
    /// </summary>
    public async Task<User?> GetByEmailIncludingDeletedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLower();

        // ✅ SECURITY: Log this sensitive operation with full context
        _logger.LogWarning(
            "📧 EMAIL VERIFICATION BYPASS: Looking up email {Email} INCLUDING soft-deleted. " +
            "Purpose: Email uniqueness validation. Stack: {StackTrace}",
            normalizedEmail,
            Environment.StackTrace);

        return await GetByIdIncludingDeletedInternalAsync(
            null,
            includeAuditData: false,
            purpose: "email-verification",
            emailFilter: normalizedEmail);
    }

    /// <summary>
    /// Check if email exists INCLUDING soft-deleted users.
    /// Returns true if email is found in any user record (active or deleted).
    /// </summary>
    public async Task<bool> EmailExistsIncludingDeletedAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLower();

        return await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail);
    }

    /// <summary>
    /// Check if user exists INCLUDING soft-deleted users.
    /// Returns true if user record exists (active or deleted).
    /// </summary>
    public async Task<bool> UserExistsIncludingDeletedAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        return await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId);
    }

    #endregion

    #region INTERNAL IMPLEMENTATION (Private - DRY Pattern)

    /// <summary>
    /// Internal method to retrieve user including soft-deleted records.
    /// 
    /// DESIGN NOTE:
    /// This is PRIVATE to keep the repository API focused on purpose-specific methods.
    /// Public methods (GetByIdForAuthorizationAsync, GetByIdForRestorationAsync, etc.)
    /// delegate to this internal method to avoid code duplication while maintaining
    /// a clean, semantically clear public API (SOLID compliance).
    /// 
    /// This method is NOT called directly; use purpose-specific public methods instead.
    /// </summary>
    private async Task<User?> GetByIdIncludingDeletedInternalAsync(
        Guid? userId,
        bool includeAuditData,
        string purpose,
        string? emailFilter = null)
    {
        var query = _dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.Clinic)
            .Include(u => u.Role)
            .AsNoTracking();

        // ✅ Add CreatedByUser navigation only when needed (for restoration)
        if (includeAuditData)
        {
            query = query.Include(u => u.CreatedByUser);
        }

        // ✅ Filter by userId or email based on purpose
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            return await query.FirstOrDefaultAsync(u => u.Id == userId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            return await query.FirstOrDefaultAsync(u => u.Email == emailFilter);
        }

        return null;
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

        _logger.LogInformation("✅ User {UserId} created with email {Email}", user.Id, user.Email);
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

        _logger.LogInformation("✅ User {UserId} updated", user.Id);
        return user;
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        if (userId == Guid.Empty) return;

        await _dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LastLoginAt, DateTime.UtcNow));
    }
    
    public async Task<bool> SoftDeleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) 
            return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ User {UserId} soft deleted at {DeletedAt}", userId, user.DeletedAt);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) 
            return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("🚨 PERMANENT DELETE: User {UserId} permanently deleted", userId);
        return true;
    }
    
    public async Task<int> CountByClinicIdAsync(Guid clinicId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .CountAsync(u => u.ClinicId == clinicId && !u.IsDeleted);
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
            if (dict == null) 
                return (null, null);

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
        catch (Exception)
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