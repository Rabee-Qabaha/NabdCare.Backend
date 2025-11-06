import { useQuery, useQueryClient } from '@tanstack/vue-query';
import type { QueryFunction } from '@tanstack/vue-query';
import { unref, isRef, watch, type Ref } from 'vue';
import { useToast } from 'primevue/usetoast';

/**
 * ✅ Vue Query v5-compatible helper for Vue
 * Adds:
 *  - Toast messages on error/success
 *  - Optional cache invalidation on success
 *  - Supports ref() or plain options
 */
export function useQueryWithToasts<
  TQueryFnData = unknown,
  TError = unknown,
  TData = TQueryFnData,
>(options: {
  // Core Vue Query options
  queryKey: readonly unknown[];
  queryFn: QueryFunction<TQueryFnData>;
  enabled?: boolean;
  retry?: number | boolean | ((failureCount: number, error: TError) => boolean);
  retryDelay?: number | ((attemptIndex: number, error: TError) => number);
  networkMode?: 'always' | 'online' | 'offlineFirst';
  staleTime?: number;
  gcTime?: number; // deprecated but kept for backward compatibility
  cacheTime?: number; // ✅ TanStack 5 uses cacheTime
  select?: (data: TQueryFnData) => TData;
  meta?: Record<string, unknown>;

  // Toast + invalidate options
  successMessage?: string;
  errorMessage?: string;
  invalidateKeys?: unknown[][];

  // Callbacks (custom)
  onSuccess?: (data: TData) => void | Promise<void>;
  onError?: (error: TError) => void | Promise<void>;
}) {
  const opts = unref(options);
  const queryClient = useQueryClient();
  const toast = useToast();

  const {
    queryKey,
    queryFn,
    successMessage,
    errorMessage,
    invalidateKeys = [],
    onSuccess,
    onError,
    ...rest
  } = opts;

  // ✅ Safe unwrap for ref-based handlers
  const unwrapCallback = <T extends (...args: any[]) => any>(
    cb: T | Ref<T | undefined> | undefined,
  ): T | undefined => (isRef(cb) ? cb.value : cb);

  // ✅ Build the query with inferred types
  const query = useQuery<TQueryFnData, TError, TData>({
    queryKey,
    queryFn: queryFn as QueryFunction<TQueryFnData, readonly unknown[]>,
    ...rest,
  });

  // ✅ Watch for successful data fetch (first or refetch)
  watch(
    () => query.data.value,
    async (data, prev) => {
      if (
        data !== undefined &&
        data !== prev && // avoid duplicate triggers
        !query.isLoading.value &&
        !query.isError.value
      ) {
        // 🔁 Cache invalidation
        for (const key of invalidateKeys) {
          await queryClient.invalidateQueries({ queryKey: key });
        }

        // ✅ Toast success
        if (successMessage) {
          toast.add({
            severity: 'success',
            summary: 'Success',
            detail: successMessage,
            life: 3000,
          });
        }

        // 🔁 Forward user callback
        const cb = unwrapCallback(onSuccess);
        if (cb) await cb(data as TData);
      }
    },
    { flush: 'post' },
  );

  // ✅ Watch for error state
  watch(
    () => query.error.value,
    async (error, prev) => {
      if (error !== null && error !== prev && query.isError.value) {
        // ❌ Toast error
        if (errorMessage) {
          toast.add({
            severity: 'error',
            summary: 'Error',
            detail: errorMessage,
            life: 4000,
          });
        }

        const cb = unwrapCallback(onError);
        if (cb) await cb(error as TError);
      }
    },
    { flush: 'post' },
  );

  return query;
}
