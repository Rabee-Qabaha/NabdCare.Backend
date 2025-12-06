import { usersApi } from '@/api/modules/users';
import type { PaginatedResult, UserResponseDto } from '@/types/backend';
import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

function buildParams(params: {
  search?: string;
  includeDeleted?: boolean;
  clinicId?: string | null;
  cursor?: string | null;
}) {
  return {
    search: params.search || undefined,
    includeDeleted: params.includeDeleted ?? false,
    clinicId: params.clinicId || undefined,
    cursor: params.cursor,
    limit: 20,
    descending: true,
  };
}

export function useInfiniteUsersPaged(options: {
  search: Ref<string> | string;
  includeDeleted: Ref<boolean> | boolean;
  clinicId?: Ref<string | null> | string | null;
}) {
  const search = computed(() => unref(options.search));
  const includeDeleted = computed(() => unref(options.includeDeleted));
  const clinicId = computed(() => unref(options.clinicId));

  const queryKey = computed(() => [
    'users',
    'infinite',
    {
      search: search.value,
      includeDeleted: includeDeleted.value,
      clinicId: clinicId.value,
    },
  ]);

  return useInfiniteQuery<PaginatedResult<UserResponseDto>>({
    queryKey,
    queryFn: async ({ pageParam }) => {
      const { data } = await usersApi.getPaged(
        buildParams({
          search: search.value,
          includeDeleted: includeDeleted.value,
          clinicId: clinicId.value,
          cursor: pageParam as string | null,
        }),
      );
      return data;
    },
    initialPageParam: null,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    staleTime: 1000 * 60 * 5,
    placeholderData: keepPreviousData,
  });
}

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

export function useUsersPaged(options: {
  page?: number;
  limit?: number;
  search?: string;
  includeDeleted?: boolean;
}) {
  const normalized = computed(() => buildParams({ ...options, cursor: null }));

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
