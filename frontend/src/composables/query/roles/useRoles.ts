// src/composables/query/helpers/useMutationWithInvalidate.ts
import { useMutation, useQueryClient } from "@tanstack/vue-query";
import type { MutationFunction } from "@tanstack/vue-query";
import { unref, isRef, type Ref } from "vue";
import { useToast } from "primevue/usetoast";

/**
 * ✅ Vue Query v5-compatible mutation helper for Vue
 * Adds:
 *  - Toast notifications on success/error
 *  - Automatic cache invalidation (static & dynamic)
 *  - Full support for readonly tuples (no casting needed)
 */
export function useMutationWithInvalidate<
  TData = unknown,
  TError = unknown,
  TVariables = void,
  TContext = unknown,
>(options: {
  // Vue Query options
  mutationKey?: readonly unknown[];
  mutationFn: MutationFunction<TData, TVariables>;
  onSuccess?: (
    data: TData,
    variables: TVariables,
    context: TContext
  ) => Promise<unknown> | unknown;
  onError?: (
    error: TError,
    variables: TVariables,
    context?: TContext
  ) => Promise<unknown> | unknown;
  onMutate?: (variables: TVariables) => Promise<TContext> | TContext;

  // Custom options - supports both static and dynamic keys
  invalidateKeys?: readonly (
    | readonly unknown[]
    | ((variables: TVariables) => readonly unknown[])
  )[];
  successMessage?: string;
  errorMessage?: string;

  // Other Vue Query options
  throwOnError?: boolean | ((error: TError) => boolean);
  retry?: number | boolean | ((failureCount: number, error: TError) => boolean);
  retryDelay?: number | ((attemptIndex: number, error: TError) => number);
  networkMode?: "always" | "online" | "offlineFirst";
  gcTime?: number;
  meta?: Record<string, unknown>;
}) {
  const opts = unref(options) as typeof options;

  const queryClient = useQueryClient();
  const toast = useToast();

  const {
    invalidateKeys = [],
    successMessage,
    errorMessage,
    onSuccess,
    onError,
    onMutate,
    mutationFn,
    mutationKey,
    ...rest
  } = opts;

  // ✅ Safely unwrap callback if it's a Ref
  const unwrapCallback = <T extends (...args: any[]) => any>(
    callback: T | Ref<T | undefined> | undefined
  ): T | undefined => {
    if (!callback) return undefined;
    return isRef(callback)
      ? (callback.value as T | undefined)
      : (callback as T);
  };

  return useMutation<TData, TError, TVariables, TContext>({
    mutationKey: mutationKey as unknown as unknown[],
    mutationFn,
    onMutate,
    ...rest,
    async onSuccess(data, variables, context) {
      // 🔁 Invalidate cache - support both static and dynamic keys
      for (const key of invalidateKeys) {
        const resolvedKey = typeof key === "function" ? key(variables) : key;
        await queryClient.invalidateQueries({
          queryKey: resolvedKey as unknown as unknown[],
        });
      }

      // ✅ Toast success
      if (successMessage) {
        toast.add({
          severity: "success",
          summary: "Success",
          detail: successMessage,
          life: 3000,
        });
      }

      // 🔁 User callback
      const successCallback = unwrapCallback(onSuccess);
      if (successCallback) {
        await successCallback(data, variables, context as TContext);
      }
    },

    async onError(error, variables, context) {
      // ❌ Toast error
      if (errorMessage) {
        toast.add({
          severity: "error",
          summary: "Error",
          detail: errorMessage,
          life: 4000,
        });
      }

      // 🔁 User callback
      const errorCallback = unwrapCallback(onError);
      if (errorCallback) {
        await errorCallback(error, variables, context as TContext | undefined);
      }
    },
  });
}
