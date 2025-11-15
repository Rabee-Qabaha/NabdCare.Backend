// src/composables/query/users/useUsers.ts
import { usersApi, type UsersPagedParams } from '@/api/modules/users';
import { useQueryWithToasts } from '@/composables/query/helpers/useQueryWithToasts';
import type { PaginatedResult, UserResponseDto } from '@/types/backend';
import { useInfiniteQuery } from '@tanstack/vue-query';

/**
 * Helper to normalize params to UsersPagedParams
 */
function buildUsersParams(params?: Record<string, any>): UsersPagedParams {
  return {
    limit: params?.limit ?? 20,
    descending: params?.descending ?? true,
    includeDeleted: params?.includeDeleted ?? false,
    search: params?.search ?? undefined,
    cursor: params?.cursor ?? null,
    clinicId: params?.clinicId ?? undefined,
  };
}

// -------------------------------------------------
// GET PAGED USERS (single page)
// -------------------------------------------------
export function useUsersPaged(params?: Record<string, any>) {
  const normalized = buildUsersParams(params);

  return useQueryWithToasts<PaginatedResult<UserResponseDto>, Error>({
    queryKey: ['users', 'paged', normalized],
    queryFn: async () => {
      const { data } = await usersApi.getPaged(normalized);
      return data;
    },
    errorMessage: 'Failed to load users.',
    staleTime: 1000 * 60 * 5,
  });
}

// -------------------------------------------------
// INFINITE SCROLL USERS (cursor pagination)
// -------------------------------------------------
import { isRef } from 'vue';

export function useInfiniteUsersPaged(params?: { search?: any; includeDeleted?: any }) {
  return useInfiniteQuery<
    PaginatedResult<UserResponseDto>,
    Error,
    PaginatedResult<UserResponseDto>,
    any[],
    string | null
  >({
    queryKey: ['users', 'infinite', params?.search, params?.includeDeleted],

    initialPageParam: null as string | null,

    queryFn: async ({ pageParam }) => {
      const search = isRef(params?.search) ? params.search.value : params?.search;

      const includeDeleted = isRef(params?.includeDeleted)
        ? params.includeDeleted.value
        : params?.includeDeleted;

      const queryParams: UsersPagedParams = {
        limit: 20,
        descending: true,
        search: search || undefined,
        includeDeleted: includeDeleted ?? false,
        cursor: pageParam ?? null,
      };

      const { data } = await usersApi.getPaged(queryParams);
      return data;
    },

    getNextPageParam: (lastPage) => (lastPage?.hasMore ? lastPage.nextCursor : null),

    staleTime: 1000 * 60 * 5,
  });
}

// -------------------------------------------------
// GET USER BY ID
// -------------------------------------------------
export function useUser(id: string) {
  return useQueryWithToasts<UserResponseDto, Error>({
    queryKey: ['user', id],
    queryFn: async () => {
      const { data } = await usersApi.getById(id);
      return data;
    },
    enabled: !!id,
    staleTime: 1000 * 60 * 5,
    errorMessage: 'Failed to load user.',
  });
}

// -------------------------------------------------
// GET USERS BY CLINIC (infinite scroll)
// -------------------------------------------------
export function useClinicUsers(clinicId: string, params?: Record<string, any>) {
  const searchValue = params?.search;

  return useInfiniteQuery<
    PaginatedResult<UserResponseDto>,
    Error,
    PaginatedResult<UserResponseDto>,
    readonly unknown[],
    string | null
  >({
    queryKey: ['users', 'clinic', clinicId, searchValue],

    queryFn: async ({ pageParam }) => {
      const queryParams: UsersPagedParams = {
        limit: 20,
        descending: true,
        search: searchValue || undefined,
        cursor: pageParam ?? null,
      };

      const { data } = await usersApi.getByClinicPaged(clinicId, queryParams);
      return data;
    },

    getNextPageParam: (lastPage) => (lastPage?.hasMore ? (lastPage.nextCursor ?? null) : null),

    initialPageParam: null,
    staleTime: 1000 * 60 * 5,
    placeholderData: (prev) => prev,
  });
}
