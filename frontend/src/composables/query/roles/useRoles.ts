// src/composables/query/roles/useRoles.ts
import { rolesApi } from '@/api/modules/roles';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { useRoleFilters } from '@/composables/role/useRoleFilters';
import type { RoleResponseDto } from '@/types/backend';
import { useQuery } from '@tanstack/vue-query';
import { computed, ref } from 'vue';

export function useRoles(options?: { includeDeleted?: boolean; clinicId?: string | null }) {
  const includeDeleted = ref(options?.includeDeleted ?? false);
  const clinicId = ref(options?.clinicId ?? null);

  const { handleErrorAndNotify } = useErrorHandler();

  // ============================================
  // 🔑 Build the query key reactively
  // ============================================
  const queryKey = computed(() => [
    'roles',
    {
      includeDeleted: includeDeleted.value,
      clinicId: clinicId.value,
    },
  ]);

  // ============================================
  // 🔍 Main Vue Query request
  // ============================================
  const query = useQuery<RoleResponseDto[]>({
    queryKey,

    queryFn: async () => {
      try {
        // rolesApi expects single object
        const response = await rolesApi.getAll({
          includeDeleted: includeDeleted.value,
          clinicId: clinicId.value ?? undefined,
        });

        return response.data;
      } catch (err) {
        handleErrorAndNotify(err);
        throw err;
      }
    },

    // ============================
    // 🌀 SWR behaviour
    // ============================
    staleTime: 1000 * 60 * 5, // data considered fresh for 5 minutes
    gcTime: 1000 * 60 * 30, // garbage collect after 30 mins
    placeholderData: (prev) => prev, // use cached data instantly (SWR)
    refetchOnWindowFocus: true,
    retry: 1,
  });

  // Data reactive
  const roles = computed(() => query.data.value ?? []);

  // ============================================
  // 🔍 Client-side filters
  // ============================================
  const { activeFilters, filteredRoles, resetFilters } = useRoleFilters(roles);

  return {
    // Data
    roles,
    filteredRoles,

    // Filters
    activeFilters,
    resetFilters,

    // State
    isLoading: query.isLoading,
    isFetching: query.isFetching,
    error: query.error,

    // Controls
    refetch: query.refetch,
    includeDeleted,
    clinicId,
  };
}
