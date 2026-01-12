import { rolesApi } from '@/api/modules/roles';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import type { PaginatedResult, RoleFilterRequestDto, RoleResponseDto } from '@/types/backend';
import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

// =================================================================
// 1. Interfaces & Helpers
// =================================================================

export interface RoleQueryFilters {
  search: Ref<string> | string;
  clinicId?: Ref<string | null> | string | null;
  roleOrigin?: Ref<string | null> | string | null; // 'system' | 'clinic' | 'template'
  isTemplate?: Ref<boolean | null> | boolean | null;
  status?: Ref<string | null> | string | null; // 'active' | 'deleted' | 'all'
  dateRange?: Ref<Date[] | null> | Date[] | null;
}

/**
 * Maps UI Filter State -> Backend DTO (RoleFilterRequestDto)
 */
function buildParams(params: any): RoleFilterRequestDto {
  const dto: RoleFilterRequestDto = {
    cursor: params.cursor || undefined,
    limit: params.limit || 20,
    descending: true,

    // Filters
    search: params.search || undefined,
    clinicId: params.clinicId || undefined,

    // Map 'status' ('active', 'deleted', 'all') to includeDeleted
    // If status is 'deleted' or 'all', we must ask backend for deleted items
    includeDeleted: params.status === 'deleted' || params.status === 'all',

    // Map UI "Origin" dropdown to Backend 'RoleOrigin' string
    roleOrigin: params.roleOrigin || undefined,

    // Map UI "Usage" dropdown to IsTemplate boolean
    isTemplate: params.isTemplate ?? undefined,
    isSystemRole: undefined, // Optional: You can map this if you have a specific UI toggle

    // Date Range Mapping
    fromDate: undefined,
    toDate: undefined,
    sortBy: '',
    filter: '',
  };

  if (params.dateRange && Array.isArray(params.dateRange) && params.dateRange.length === 2) {
    if (params.dateRange[0]) dto.fromDate = new Date(params.dateRange[0]);
    if (params.dateRange[1]) dto.toDate = new Date(params.dateRange[1]);
  }

  return dto;
}

// =================================================================
// 🚀 2. INFINITE SCROLL HOOK (For Management Grid)
// =================================================================
export function useInfiniteRolesPaged(filters: RoleQueryFilters) {
  const { handleErrorAndNotify } = useErrorHandler();

  // Unwrap refs for reactivity tracking
  const search = computed(() => unref(filters.search));
  const clinicId = computed(() => unref(filters.clinicId));
  const roleOrigin = computed(() => unref(filters.roleOrigin));
  const isTemplate = computed(() => unref(filters.isTemplate));
  const status = computed(() => unref(filters.status));
  const dateRange = computed(() => unref(filters.dateRange));

  // Reactive Query Key: Any change here triggers a refetch
  const queryKey = computed(() => [
    'roles',
    'infinite',
    {
      search: search.value,
      clinicId: clinicId.value,
      roleOrigin: roleOrigin.value,
      isTemplate: isTemplate.value,
      status: status.value,
      dateRange: dateRange.value,
    },
  ]);

  return useInfiniteQuery<PaginatedResult<RoleResponseDto>>({
    queryKey,
    queryFn: async ({ pageParam }) => {
      try {
        const { data } = await rolesApi.getPaged(
          buildParams({
            cursor: pageParam as string | null,
            search: search.value,
            clinicId: clinicId.value,
            roleOrigin: roleOrigin.value,
            isTemplate: isTemplate.value,
            status: status.value,
            dateRange: dateRange.value,
          }),
        );
        return data;
      } catch (err) {
        handleErrorAndNotify(err);
        throw err;
      }
    },
    initialPageParam: null,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    staleTime: 1000 * 60 * 5, // Cache for 5 mins
    placeholderData: keepPreviousData, // Smooth transitions
    retry: 1,
  });
}

// =================================================================
// 📋 3. SIMPLE LIST HOOK (For Dropdowns)
// =================================================================
export function useAllRoles(options?: {
  clinicId?: Ref<string | null> | string | null;
  isTemplate?: boolean;
}) {
  const { handleErrorAndNotify } = useErrorHandler();

  const clinicId = computed(() => unref(options?.clinicId));
  const isTemplate = computed(() => unref(options?.isTemplate));

  return useQuery({
    queryKey: ['roles', 'all', { clinicId, isTemplate }],
    queryFn: async () => {
      try {
        // ✅ FIX: Include default pagination fields to satisfy Backend [AsParameters] binding
        const payload = {
          clinicId: clinicId.value || undefined,
          isTemplate: isTemplate.value ?? undefined,
          includeDeleted: false,

          // Required defaults for PaginationRequestDto
          limit: 100, // Fetch plenty for dropdowns
          descending: true,
          sortBy: 'DisplayOrder',
          filter: undefined,
          cursor: undefined,
          roleOrigin: undefined,
          search: undefined,
          fromDate: undefined,
          toDate: undefined,
        } as unknown as RoleFilterRequestDto;

        const { data } = await rolesApi.getAll(payload);
        return data;
      } catch (err) {
        handleErrorAndNotify(err);
        throw err;
      }
    },
    staleTime: 1000 * 60 * 5,
  });
}
