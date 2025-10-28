using Microsoft.EntityFrameworkCore;
using NabdCare.Application.Common;
using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Application.Interfaces.Audit;
using NabdCare.Domain.Entities.Audits;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Infrastructure.Repositories.Audit;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly NabdCareDbContext _db;
    private readonly ITenantContext _tenant;

    public AuditLogRepository(NabdCareDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task CreateAsync(AuditLog log)
    {
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
    
    public async Task<(IEnumerable<AuditLogResponseDto> items, int total)>
        GetAuditLogsAsync(AuditLogListRequestDto req)
    {
        var isSuperAdmin = _tenant.IsSuperAdmin;
        var tenantClinicId = _tenant.ClinicId;

        // ✅ SECURITY GUARD:
        // Block queries for userId belonging to another clinic
        if (!isSuperAdmin && req.UserId.HasValue)
        {
            bool belongsToClinic = await _db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u =>
                    u.Id == req.UserId.Value &&
                    u.ClinicId == tenantClinicId &&
                    !u.IsDeleted &&
                    u.IsActive);

            if (!belongsToClinic)
            {
                return (Enumerable.Empty<AuditLogResponseDto>(), 0);
            }
        }

        var query = _db.AuditLogs.AsQueryable();

        // Filtering
        if (!string.IsNullOrWhiteSpace(req.Action))
            query = query.Where(a => a.Action == req.Action);

        if (!string.IsNullOrWhiteSpace(req.EntityType))
            query = query.Where(a => a.EntityType == req.EntityType);

        if (req.UserId.HasValue)
            query = query.Where(a => a.UserId == req.UserId.Value);

        if (req.Start.HasValue)
            query = query.Where(a => a.Timestamp >= req.Start);

        if (req.End.HasValue)
            query = query.Where(a => a.Timestamp <= req.End);

        if (!string.IsNullOrWhiteSpace(req.Search))
            query = query.Where(a =>
                (a.Reason != null && a.Reason.Contains(req.Search)) ||
                (a.Changes != null && a.Changes.Contains(req.Search))
            );

        var total = await query.CountAsync();

        int skip = (req.Page - 1) * Math.Min(req.PageSize, 100);
        int take = Math.Min(req.PageSize, 100);

        var data = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(a => new AuditLogResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                ClinicId = a.ClinicId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                Changes = a.Changes,
                Reason = a.Reason,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent
            })
            .ToListAsync();

        return (data, total);
    }
}