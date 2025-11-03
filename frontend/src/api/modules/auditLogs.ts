import { api } from "@/api/apiClient";
import type { PaginatedResult, PaginationRequestDto } from "@/types/backend";
import type {
  AuditLogListRequestDto,
  AuditLogResponseDto,
} from "@/types/backend/index";

export const auditLogsApi = {
  /**
   * Get audit logs (supports cursor-based pagination and filters)
   */
  async getPaged(
    filters: AuditLogListRequestDto,
    pagination: PaginationRequestDto
  ) {
    const { data } = await api.get<PaginatedResult<AuditLogResponseDto>>(
      "/audit-logs",
      {
        params: {
          ...filters,
          ...pagination,
        },
      }
    );
    return data;
  },
};
