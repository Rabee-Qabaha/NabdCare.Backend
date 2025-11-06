// src/composables/query/helpers/useMutationWithInvalidate.ts
import type { MutationFunction } from '@tanstack/vue-query';
import { useMutation, useQueryClient } from '@tanstack/vue-query';
import { useToast } from 'primevue/usetoast';
import { isRef, unref, type Ref } from 'vue';

/**
 * ✅ Vue Query v5-compatible helper for Vue
 * Adds:
 *  - Toast messages
 *  - Automatic cache invalidation
 *  - Supports `ref()` or plain object options
 */
export function useMutationWithInvalidate<
  TData = unknown,
  TError = unknown,
  TVariables = void,
  TContext = unknown,
>(options: {
  // Vue Query options
  mutationFn?: MutationFunction<TData, TVariables>;
  mutationKey?: readonly unknown[];
  onSuccess?: (data: TData, variables: TVariables, context: TContext) => Promise<unknown> | unknown;
  onError?: (
    error: TError,
    variables: TVariables,
    context?: TContext,
  ) => Promise<unknown> | unknown;
  onMutate?: (variables: TVariables) => Promise<TContext> | TContext;

  // Custom options - now accepts readonly arrays and functions
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
  networkMode?: 'always' | 'online' | 'offlineFirst';
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

  // Safely unwrap callback if it's a Ref
  const unwrapCallback = <T extends (...args: any[]) => any>(
    callback: T | Ref<T | undefined> | undefined,
  ): T | undefined => {
    if (!callback) return undefined;
    return isRef(callback) ? (callback.value as T | undefined) : (callback as T);
  };

  return useMutation<TData, TError, TVariables, TContext>({
    mutationFn,
    mutationKey: mutationKey as unknown[] | undefined,
    onMutate,
    ...rest,
    async onSuccess(data, variables, context) {
      // Invalidate cache - support both static and dynamic keys
      for (const key of invalidateKeys) {
        const resolvedKey = typeof key === 'function' ? key(variables) : key;
        await queryClient.invalidateQueries({
          queryKey: resolvedKey as unknown[],
        });
      }

      // Toast success
      if (successMessage) {
        toast.add({
          severity: 'success',
          summary: 'Success',
          detail: successMessage,
          life: 3000,
        });
      }

      // Call original handler if provided
      const successCallback = unwrapCallback(onSuccess);
      if (successCallback) {
        await successCallback(data, variables, context as TContext);
      }
    },

    async onError(error, variables, context) {
      // Toast error
      if (errorMessage) {
        toast.add({
          severity: 'error',
          summary: 'Error',
          detail: errorMessage,
          life: 4000,
        });
      }

      // Call original handler if provided
      const errorCallback = unwrapCallback(onError);
      if (errorCallback) {
        await errorCallback(error, variables, context as TContext | undefined);
      }
    },
  });
}
