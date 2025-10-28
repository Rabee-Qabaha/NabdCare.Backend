using NabdCare.Application.DTOs.AuditLogs;
using NabdCare.Domain.Entities.Audits;

namespace NabdCare.Application.Interfaces.Audit;
public interface IAuditLogRepository
{
    Task<(IEnumerable<AuditLogResponseDto> items, int total)> GetAuditLogsAsync(
        AuditLogListRequestDto filter);
    Task CreateAsync(AuditLog log);
}