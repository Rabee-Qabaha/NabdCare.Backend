// src/composables/query/users/useUsers.ts
// import { usersApi } from '@/api/modules/users';
// import type { PaginatedResult, UserResponseDto } from '@/types/backend';
// import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/vue-query';
// import { computed, unref, type Ref } from 'vue';

// // 1. Updated: Added 'limit' to arguments and implementation
// function buildParams(params: {
//   search?: string;
//   includeDeleted?: boolean;
//   clinicId?: string | null;
//   cursor?: string | null;
//   limit?: number;
// }) {
//   return {
//     search: params.search || undefined,
//     includeDeleted: params.includeDeleted ?? false,
//     clinicId: params.clinicId || undefined,
//     cursor: params.cursor,
//     // FIX: Use the passed limit, or fallback to 20
//     limit: params.limit ?? 20,
//     descending: true,
//   };
// }

// export function useInfiniteUsersPaged(options: {
//   search: Ref<string> | string;
//   includeDeleted: Ref<boolean> | boolean;
//   clinicId?: Ref<string | null> | string | null;
// }) {
//   const search = computed(() => unref(options.search));
//   const includeDeleted = computed(() => unref(options.includeDeleted));
//   const clinicId = computed(() => unref(options.clinicId));

//   const queryKey = computed(() => [
//     'users',
//     'infinite',
//     {
//       search: search.value,
//       includeDeleted: includeDeleted.value,
//       clinicId: clinicId.value,
//     },
//   ]);

//   return useInfiniteQuery<PaginatedResult<UserResponseDto>>({
//     queryKey,
//     queryFn: async ({ pageParam }) => {
//       const { data } = await usersApi.getPaged(
//         buildParams({
//           search: search.value,
//           includeDeleted: includeDeleted.value,
//           clinicId: clinicId.value,
//           cursor: pageParam as string | null,
//         }),
//       );
//       return data;
//     },
//     initialPageParam: null,
//     getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
//     staleTime: 1000 * 60 * 5,
//     refetchOnWindowFocus: true,
//     placeholderData: keepPreviousData,
//   });
// }

// export function useUser(userId: Ref<string | null>) {
//   return useQuery({
//     queryKey: computed(() => ['user', userId.value]),
//     queryFn: async () => {
//       if (!userId.value) return null;
//       const { data } = await usersApi.getById(userId.value);
//       return data;
//     },
//     enabled: computed(() => !!userId.value),
//     staleTime: 1000 * 60 * 5,
//   });
// }

// // 2. Updated: Added 'clinicId' to the interface
// export function useUsersPaged(options: {
//   page?: number;
//   limit?: number;
//   search?: string;
//   includeDeleted?: boolean;
//   clinicId?: string | null;
// }) {
//   // Pass options directly since we updated buildParams signature
//   const normalized = computed(() => buildParams({ ...options, cursor: null }));

//   return useQuery({
//     queryKey: ['users', 'paged', normalized],
//     queryFn: async () => {
//       const { data } = await usersApi.getPaged(normalized.value);
//       return data;
//     },
//     staleTime: 1000 * 60 * 5,
//     placeholderData: keepPreviousData,
//   });
// }

// src/composables/query/users/useUsers.ts
import { usersApi } from '@/api/modules/users';
import type { PaginatedResult, UserFilterRequestDto, UserResponseDto } from '@/types/backend';
import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

// 1. Interface for Reactive Inputs
// This defines what the Composable accepts from your Vue Components
export interface UserQueryFilters {
  search: Ref<string> | string;
  roleId?: Ref<string | null> | string | null;
  clinicId?: Ref<string | null> | string | null;
  isActive?: Ref<boolean | null> | boolean | null;
  includeDeleted?: Ref<boolean> | boolean;
  dateRange?: Ref<Date[] | null> | Date[] | null;
}

// 2. Helper: Transform UI Params -> Backend DTO
// This maps the Vue state (e.g., date array) to the DTO structure (fromDate, toDate)
function buildParams(params: any): UserFilterRequestDto {
  let fromDate: Date | undefined;
  let toDate: Date | undefined;

  if (params.dateRange && Array.isArray(params.dateRange) && params.dateRange.length === 2) {
    if (params.dateRange[0]) fromDate = new Date(params.dateRange[0]);
    if (params.dateRange[1]) toDate = new Date(params.dateRange[1]);
  }

  // 1. Create a plain object that matches the shape we want
  const payload = {
    cursor: params.cursor || undefined,
    limit: params.limit || 20,
    descending: true,
    search: params.search || undefined,
    roleId: params.roleId || undefined,
    clinicId: params.clinicId || undefined,
    isActive: params.isActive ?? undefined,
    includeDeleted: params.includeDeleted ?? false,
    fromDate: fromDate,
    toDate: toDate,
  };

  // 2. Force cast it to the DTO type to silence errors
  // This bypasses the strict checks for 'Date | undefined'
  return payload as unknown as UserFilterRequestDto;
}

// ==========================================
// 🚀 1. INFINITE SCROLL (For Management Grid)
// ==========================================
export function useInfiniteUsersPaged(filters: UserQueryFilters) {
  // Normalize Refs to standard values
  const search = computed(() => unref(filters.search));
  const roleId = computed(() => unref(filters.roleId));
  const clinicId = computed(() => unref(filters.clinicId));
  const isActive = computed(() => unref(filters.isActive));
  const includeDeleted = computed(() => unref(filters.includeDeleted));
  const dateRange = computed(() => unref(filters.dateRange));

  // Reactive Query Key
  // TanStack Query watches this array. Any change triggers a refetch.
  const queryKey = computed(() => [
    'users',
    'infinite',
    {
      search: search.value,
      roleId: roleId.value,
      clinicId: clinicId.value,
      isActive: isActive.value,
      includeDeleted: includeDeleted.value,
      dateRange: dateRange.value,
    },
  ]);

  return useInfiniteQuery<PaginatedResult<UserResponseDto>>({
    queryKey,
    queryFn: async ({ pageParam }) => {
      // Build DTO and call API
      const { data } = await usersApi.getPaged(
        buildParams({
          cursor: pageParam as string | null,
          search: search.value,
          roleId: roleId.value,
          clinicId: clinicId.value,
          isActive: isActive.value,
          includeDeleted: includeDeleted.value,
          dateRange: dateRange.value,
        }),
      );
      return data;
    },
    initialPageParam: null,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    staleTime: 1000 * 60 * 5, // Cache for 5 mins
    placeholderData: keepPreviousData, // Smooth transitions (no flash of loading state)
  });
}

// ==========================================
// 👤 2. SINGLE USER FETCH
// ==========================================
export function useUser(userId: Ref<string | null>) {
  return useQuery({
    queryKey: computed(() => ['user', userId.value]),
    queryFn: async () => {
      if (!userId.value) return null;
      const { data } = await usersApi.getById(userId.value);
      return data;
    },
    enabled: computed(() => !!userId.value),
    staleTime: 1000 * 60 * 5,
  });
}

// ==========================================
// 📦 3. STANDARD PAGINATION (Widgets / Charts)
// ==========================================
// This is used by ResourceUtilizationCard.vue or similar components
export function useUsersPaged(options: {
  page?: number;
  limit?: number;
  search?: Ref<string> | string;
  includeDeleted?: Ref<boolean> | boolean;
  clinicId?: Ref<string | null> | string | null;
}) {
  const params = computed(() => {
    return {
      page: options.page,
      limit: options.limit,
      search: unref(options.search),
      includeDeleted: unref(options.includeDeleted),
      clinicId: unref(options.clinicId),
    };
  });

  const normalized = computed(() => buildParams({ ...params.value, cursor: null }));

  return useQuery({
    queryKey: ['users', 'paged', normalized],
    queryFn: async () => {
      const { data } = await usersApi.getPaged(normalized.value);
      return data;
    },
    staleTime: 1000 * 60 * 5,
    placeholderData: keepPreviousData,
  });
}
