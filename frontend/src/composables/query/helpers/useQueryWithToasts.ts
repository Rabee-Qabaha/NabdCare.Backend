import type { QueryFunction } from "@tanstack/vue-query";
import { useQuery, useQueryClient } from "@tanstack/vue-query";
import { useToast } from "primevue/usetoast";
import { isRef, unref, watch, type Ref } from "vue";

/**
 * ✅ Vue Query v5-compatible helper
 * Adds:
 *  • Toasts on success / error
 *  • Optional cache invalidation
 *  • Works with refs or plain values
 */
export function useQueryWithToasts<
  TQueryFnData = unknown,
  TError = unknown,
  TData = TQueryFnData,
>(options: {
  queryKey: readonly unknown[];
  queryFn: QueryFunction<TQueryFnData>;
  enabled?: boolean;
  retry?: number | boolean | ((failureCount: number, error: TError) => boolean);
  retryDelay?: number | ((attemptIndex: number, error: TError) => number);
  networkMode?: "always" | "online" | "offlineFirst";
  staleTime?: number;
  gcTime?: number;
  cacheTime?: number;
  select?: (data: TQueryFnData) => TData;
  meta?: Record<string, unknown>;

  successMessage?: string;
  errorMessage?: string;
  invalidateKeys?: unknown[][];

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

  const unwrapCallback = <T extends (...args: any[]) => any>(
    cb: T | Ref<T | undefined> | undefined,
  ): T | undefined => (isRef(cb) ? cb.value : cb);

  const query = useQuery<TQueryFnData, TError, TData>({
    queryKey,
    queryFn: queryFn as QueryFunction<TQueryFnData, readonly unknown[]>,
    ...rest,
  });

  // 🔍 watch for success
  watch(
    () => query.data.value,
    async (data, prev) => {
      if (data !== undefined && data !== prev && !query.isLoading.value && !query.isError.value) {
        for (const key of invalidateKeys) {
          await queryClient.invalidateQueries({ queryKey: key });
        }
        if (successMessage)
          toast.add({ severity: "success", summary: "Success", detail: successMessage, life: 3000 });
        const cb = unwrapCallback(onSuccess);
        if (cb) await cb(data as TData);
      }
    },
    { flush: "post" },
  );

  // ❌ watch for error
  watch(
    () => query.error.value,
    async (error, prev) => {
      if (error !== null && error !== prev && query.isError.value) {
        if (errorMessage)
          toast.add({ severity: "error", summary: "Error", detail: errorMessage, life: 4000 });
        const cb = unwrapCallback(onError);
        if (cb) await cb(error as TError);
      }
    },
    { flush: "post" },
  );

  return query;
}