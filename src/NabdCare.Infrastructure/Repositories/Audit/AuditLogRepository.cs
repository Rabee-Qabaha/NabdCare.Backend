using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Application.Interfaces.Audit;
using NabdCare.Domain.Entities.Audits;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Audit;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly NabdCareDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public AuditLogRepository(NabdCareDbContext db, ITenantContext tenant, IMapper mapper)
    {
        _db = db;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task CreateAsync(AuditLog log)
    {
        await _db.AuditLogs.AddAsync(log);
        await _db.SaveChangesAsync();
    }

    public async Task<PaginatedResult<AuditLogResponseDto>> GetPagedAsync(
        AuditLogListRequestDto req,
        PaginationRequestDto pagination,
        Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? abacFilter = null,
        CancellationToken cancellationToken = default)
    {
        var isSuperAdmin = _tenant.IsSuperAdmin;
        var tenantClinicId = _tenant.ClinicId;

        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking();

        // ============================================
        // 🔒 Tenant Security (Multi-Tenant Enforcement)
        // ============================================
        if (!isSuperAdmin)
        {
            query = query.Where(a => a.ClinicId == tenantClinicId);

            if (req.UserId.HasValue)
            {
                bool belongsToClinic = await _db.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(u =>
                        u.Id == req.UserId.Value &&
                        u.ClinicId == tenantClinicId &&
                        !u.IsDeleted &&
                        u.IsActive,
                        cancellationToken);

                if (!belongsToClinic)
                {
                    return new PaginatedResult<AuditLogResponseDto>
                    {
                        Items = [],
                        TotalCount = 0,
                        HasMore = false,
                        NextCursor = null
                    };
                }
            }
        }

        // ============================================
        // 🔍 Filtering
        // ============================================
        if (!string.IsNullOrWhiteSpace(req.Action))
            query = query.Where(a => a.Action == req.Action);

        if (!string.IsNullOrWhiteSpace(req.EntityType))
            query = query.Where(a => a.EntityType == req.EntityType);

        if (req.UserId.HasValue)
            query = query.Where(a => a.UserId == req.UserId.Value);

        if (req.Start.HasValue)
            query = query.Where(a => a.Timestamp >= req.Start.Value);

        if (req.End.HasValue)
            query = query.Where(a => a.Timestamp <= req.End.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.ToLower();
            query = query.Where(a =>
                (a.Reason != null && a.Reason.ToLower().Contains(search)) ||
                (a.Changes != null && a.Changes.ToLower().Contains(search)) ||
                (a.UserEmail != null && a.UserEmail.ToLower().Contains(search))
            );
        }

        // ============================================
        // 🔐 Apply ABAC dynamic filters (if provided)
        // ============================================
        if (abacFilter != null)
        {
            query = abacFilter(query);
        }

        // ============================================
        // ⚙️ Sorting
        // ============================================
        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            query = pagination.SortBy.ToLower() switch
            {
                "action" => pagination.Descending
                    ? query.OrderByDescending(a => a.Action)
                    : query.OrderBy(a => a.Action),
                "entitytype" => pagination.Descending
                    ? query.OrderByDescending(a => a.EntityType)
                    : query.OrderBy(a => a.EntityType),
                "useremail" => pagination.Descending
                    ? query.OrderByDescending(a => a.UserEmail)
                    : query.OrderBy(a => a.UserEmail),
                _ => pagination.Descending
                    ? query.OrderByDescending(a => a.Timestamp)
                    : query.OrderBy(a => a.Timestamp)
            };
        }
        else
        {
            query = query.OrderByDescending(a => a.Timestamp);
        }

        // ============================================
        // ⏭️ Cursor-based Pagination
        // ============================================
        if (!string.IsNullOrEmpty(pagination.Cursor) &&
            Guid.TryParse(pagination.Cursor, out var cursorId))
        {
            var cursorEntity = await _db.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == cursorId, cancellationToken);

            if (cursorEntity != null)
            {
                query = query.Where(a => a.Timestamp < cursorEntity.Timestamp);
            }
        }

        // ============================================
        // 🧮 Pagination and Mapping
        // ============================================
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Take(pagination.Limit + 1)
            .ProjectTo<AuditLogResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        bool hasMore = items.Count > pagination.Limit;
        string? nextCursor = hasMore ? items.Last().Id.ToString() : null;

        if (hasMore)
            items.RemoveAt(items.Count - 1);

        return new PaginatedResult<AuditLogResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }
}