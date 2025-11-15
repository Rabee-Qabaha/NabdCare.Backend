// src/composables/query/roles/useRoles.ts
import { useQuery } from "@tanstack/vue-query";
import { ref, computed } from "vue";
import type { RoleResponseDto } from "@/types/backend";
import { rolesApi } from "@/api/modules/roles";
import { useRoleFilters } from "@/composables/role/useRoleFilters";
import { useErrorHandler } from "@/composables/errorHandling/useErrorHandler";

export function useRoles(options?: {
  includeDeleted?: boolean;
  clinicId?: string | null;
}) {
  const roles = ref<RoleResponseDto[]>([]);
  const includeDeleted = ref(options?.includeDeleted ?? false);
  const clinicId = ref(options?.clinicId ?? null);

  const { handleErrorAndNotify } = useErrorHandler();

  const query = useQuery({
    queryKey: ["roles", { includeDeleted: includeDeleted.value, clinicId: clinicId.value }],
    queryFn: async () => {
      try {
        const response = await rolesApi.getAll({
          includeDeleted: includeDeleted.value,
          clinicId: clinicId.value,
        });

        // FIX: unwrap AxiosResponse if necessary
        const data = (response?.data ?? response) as RoleResponseDto[];

        roles.value = data;
        return data;
      } catch (error) {
        handleErrorAndNotify(error);
        throw error;
      }
    },
    staleTime: 1000 * 60 * 5,
    placeholderData: (prev) => prev,
  });

  const { filters, filteredRoles, resetFilters, clearFilter } = useRoleFilters(roles);

  const totalCount = computed(() => filteredRoles.value.length);

  return {
    roles,
    filteredRoles,
    filters,
    isLoading: query.isLoading,
    error: query.error,
    refetch: query.refetch,
    totalCount,
    includeDeleted,
    clinicId,
    resetFilters,
    clearFilter,
  };
}