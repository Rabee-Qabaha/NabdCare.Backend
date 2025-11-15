// src/composables/query/helpers/useCursorPagination.ts
import { useInfiniteQuery } from "@tanstack/vue-query";

/**
 * useCursorPagination
 * Generic helper for cursor-based infinite queries (Vue Query v5).
 *
 * - Adds required `initialPageParam`
 * - Preserves previous pages via placeholderData
 * - Lets Vue Query infer the correct InfiniteData return type
 */
export function useCursorPagination<TPage, TError = unknown>(
  key: readonly unknown[],
  fetchFn: (cursor?: string | null) => Promise<TPage>,
  options?: Partial<{
    enabled: boolean;
    staleTime: number;
    gcTime: number;
    retry: number | boolean | ((failureCount: number, error: TError) => boolean);
    retryDelay: number | ((attemptIndex: number, error: TError) => number);
    meta: Record<string, unknown>;
    placeholderData: (prev: unknown) => unknown;
    // add any other fields you routinely use; we keep it permissive
  }>,
) {
  return useInfiniteQuery<TPage, TError>({
    queryKey: key,
    queryFn: ({ pageParam }) => fetchFn((pageParam as string | null) ?? null),
    getNextPageParam: (lastPage: any) => lastPage?.nextCursor ?? null,
    initialPageParam: null, // required in v5
    placeholderData: (prev) => prev,
    staleTime: 1000 * 60 * 5,
    ...(options as any),
  });
}