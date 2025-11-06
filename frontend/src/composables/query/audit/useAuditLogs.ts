import { auditLogsApi } from '@/api/modules/auditLogs';
import { useQueryWithToasts } from '@/composables/query/helpers/useQueryWithToasts';
import type {
  AuditLogListRequestDto,
  AuditLogResponseDto,
  PaginatedResult,
  PaginationRequestDto,
} from '@/types/backend';

/* 🔹 Cache key factory */
export const auditLogKeys = {
  all: ['audit-logs'] as const,
  paged: (filters: AuditLogListRequestDto, pagination: PaginationRequestDto) =>
    ['audit-logs', filters, pagination] as const,
};

/* ✅ Fetch paged audit logs */
export function useAuditLogsPaged(
  filters: AuditLogListRequestDto,
  pagination: PaginationRequestDto,
) {
  return useQueryWithToasts<PaginatedResult<AuditLogResponseDto>>({
    queryKey: auditLogKeys.paged(filters, pagination),
    queryFn: () => auditLogsApi.getPaged(filters, pagination),
    successMessage: 'Audit logs loaded successfully.',
    errorMessage: 'Failed to load audit logs.',
  });
}
