import {
  useQuery,
  useMutation,
  useQueryClient,
  useInfiniteQuery,
} from "@tanstack/vue-query";
import { userApi } from "@/api/modules/users";
import type {
  CreateUserRequestDto,
  UpdateUserRequestDto,
  UserResponseDto,
  PaginatedResult,
} from "@/types/backend";

/**
 * User Query Composables
 * Location: src/composables/query/users/useUsers.ts
 *
 * Purpose:
 * - Queries for fetching user data (read operations)
 * - Mutations for creating and updating users
 * - Infinite scroll pagination support
 *
 * Note:
 * - User action mutations (activate, deactivate, delete, password)
 *   are in useUserActions.ts
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

// ========================================
// QUERIES
// ========================================

/**
 * Fetch all users (paged) - Single page
 */
export function useUsersPaged(params?: Record<string, any>) {
  return useQuery<PaginatedResult<UserResponseDto>, Error>({
    queryKey: ["users", "paged", params],
    queryFn: async () => {
      const { data } = await userApi.getPaged(params);
      return data;
    },
    staleTime: 60_000, // 1 min cache
    placeholderData: (prev) => prev,
  });
}

/**
 * Infinite loading hook for Users (with cursor pagination)
 *
 * Features:
 * - Cursor-based pagination
 * - Client-side filtering
 * - Automatic cache management
 *
 * Usage:
 * ```typescript
 * const { data, fetchNextPage, hasNextPage } = useInfiniteUsersPaged();
 * ```
 */
export function useInfiniteUsersPaged(params?: Record<string, any>) {
  return useInfiniteQuery<PaginatedResult<UserResponseDto>, Error>({
    queryKey: ["users", "infinite", params],
    queryFn: async ({ pageParam }) => {
      const queryParams = {
        ...(params?.search ? { search: params.search } : {}),
        ...(pageParam ? { cursor: pageParam } : {}),
      };

      console.log("📍 Fetching users with params:", queryParams);

      const { data } = await userApi.getPaged(queryParams);

      console.log("✅ Users fetched:", {
        count: data.items?.length,
        hasMore: data.hasMore,
        nextCursor: data.nextCursor,
      });

      return data;
    },
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.nextCursor : undefined,
    initialPageParam: undefined,
    staleTime: 1000 * 60 * 5, // 5 min cache
    placeholderData: (prev) => prev,
  });
}

/**
 * Fetch single user by ID
 */
export function useUser(id: string) {
  return useQuery<UserResponseDto, Error>({
    queryKey: ["user", id],
    queryFn: async () => {
      const { data } = await userApi.getById(id);
      return data;
    },
    enabled: !!id,
    staleTime: 1000 * 60 * 5, // 5 min cache
  });
}

/**
 * Get users by clinic (SuperAdmin only)
 */
export function useClinicUsers(clinicId: string, params?: Record<string, any>) {
  return useInfiniteQuery<PaginatedResult<UserResponseDto>, Error>({
    queryKey: ["users", "clinic", clinicId, params],
    queryFn: async ({ pageParam }) => {
      const queryParams = {
        ...(params?.search ? { search: params.search } : {}),
        ...(pageParam ? { cursor: pageParam } : {}),
      };

      const { data } = await userApi.getByClinicPaged(clinicId, queryParams);
      return data;
    },
    getNextPageParam: (lastPage) =>
      lastPage.hasMore ? lastPage.nextCursor : undefined,
    initialPageParam: undefined,
    staleTime: 1000 * 60 * 5,
    placeholderData: (prev) => prev,
  });
}

// ========================================
// MUTATIONS
// ========================================

/**
 * Create user
 */
export function useCreateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["user", "create"],
    mutationFn: (data: CreateUserRequestDto) => userApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (error: any) => {
      console.error("❌ Failed to create user:", error);
    },
  });
}

/**
 * Update user (profile, role, clinic assignment)
 */
export function useUpdateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["user", "update"],
    mutationFn: (payload: { id: string; data: UpdateUserRequestDto }) =>
      userApi.update(payload.id, payload.data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["user", id] });
    },
    onError: (error: any) => {
      console.error("❌ Failed to update user:", error);
    },
  });
}
