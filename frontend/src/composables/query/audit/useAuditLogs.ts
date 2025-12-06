import { auditLogsApi } from '@/api/modules/auditLogs';
import type {
  AuditLogListRequestDto,
  AuditLogResponseDto,
  PaginatedResult,
  PaginationRequestDto,
} from '@/types/backend';
import { keepPreviousData, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

/* 🔹 Cache key factory */
export const auditLogKeys = {
  all: ['audit-logs'] as const,
  paged: (filters: AuditLogListRequestDto, pagination: PaginationRequestDto) =>
    ['audit-logs', filters, pagination] as const,
};

/* ✅ Fetch paged audit logs */
export function useAuditLogsPaged(
  filters: Ref<AuditLogListRequestDto> | AuditLogListRequestDto,
  pagination: Ref<PaginationRequestDto> | PaginationRequestDto,
) {
  return useQuery<PaginatedResult<AuditLogResponseDto>>({
    // 1. Reactive Key (re-fetches when filters/page change)
    queryKey: computed(() => auditLogKeys.paged(unref(filters), unref(pagination))),

    // 2. Fetch Function
    queryFn: async () => {
      const currentFilters = unref(filters);
      const currentPage = unref(pagination);

      const { data } = await auditLogsApi.getPaged(currentFilters, currentPage);
      return data;
    },

    // 3. Config
    staleTime: 1000 * 60 * 1,
    placeholderData: keepPreviousData,
  });
}
