using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Application.DTOs.Pagination;
using NabdCare.Domain.Entities.Audits;

namespace NabdCare.Application.Interfaces.Audit;

public interface IAuditLogRepository
{
    /// <summary>
    /// Create a new audit log entry.
    /// </summary>
    Task CreateAsync(AuditLog log);

    /// <summary>
    /// Get audit logs with pagination and advanced filtering.
    /// Optionally allows ABAC filters to modify the query dynamically.
    /// </summary>
    Task<PaginatedResult<AuditLogResponseDto>> GetPagedAsync(
        AuditLogListRequestDto filter,
        PaginationRequestDto pagination,
        Func<IQueryable<AuditLog>, IQueryable<AuditLog>>? abacFilter = null,
        CancellationToken cancellationToken = default);
}