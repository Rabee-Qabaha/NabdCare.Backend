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
    /// </summary>
    Task<PaginatedResult<AuditLogResponseDto>> GetPagedAsync(AuditLogListRequestDto filter, PaginationRequestDto pagination);
}