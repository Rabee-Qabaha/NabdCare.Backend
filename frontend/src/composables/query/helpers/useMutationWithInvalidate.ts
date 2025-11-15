import type { MutationFunction } from "@tanstack/vue-query";
import { useMutation, useQueryClient } from "@tanstack/vue-query";
import { useToast } from "primevue/usetoast";
import { isRef, unref, type Ref } from "vue";

/**
 * ✅ Vue Query v5-compatible helper
 * Adds:
 *  • Toast messages
 *  • Automatic cache invalidation
 *  • Supports ref() or plain option objects
 */
export function useMutationWithInvalidate<
  TData = unknown,
  TError = unknown,
  TVariables = void,
  TContext = unknown,
>(options: {
  // Vue Query core
  mutationFn?: MutationFunction<TData, TVariables>;
  mutationKey?: readonly unknown[];
  onSuccess?: (data: TData, variables: TVariables, context: TContext) => Promise<unknown> | unknown;
  onError?: (
    error: TError,
    variables: TVariables,
    context?: TContext,
  ) => Promise<unknown> | unknown;
  onMutate?: (variables: TVariables) => Promise<TContext> | TContext;

  // Custom
  invalidateKeys?: readonly (
    | readonly unknown[]
    | ((variables: TVariables) => readonly unknown[])
  )[];
  successMessage?: string;
  errorMessage?: string;

  // Extra options
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

  const unwrapCallback = <T extends (...args: any[]) => any>(
    cb: T | Ref<T | undefined> | undefined,
  ): T | undefined => (isRef(cb) ? cb.value : cb);

  return useMutation<TData, TError, TVariables, TContext>({
    mutationFn,
    mutationKey: mutationKey as unknown[] | undefined,
    onMutate,
    ...rest,
    async onSuccess(data, variables, context) {
      // 🔁 invalidate cache
      for (const key of invalidateKeys) {
        const resolved = typeof key === "function" ? key(variables) : key;
        await queryClient.invalidateQueries({ queryKey: resolved as unknown[] });
      }

      // ✅ success toast
      if (successMessage)
        toast.add({ severity: "success", summary: "Success", detail: successMessage, life: 3000 });

      const cb = unwrapCallback(onSuccess);
      if (cb) await cb(data, variables, context as TContext);
    },
    async onError(error, variables, context) {
      if (errorMessage)
        toast.add({ severity: "error", summary: "Error", detail: errorMessage, life: 4000 });

      const cb = unwrapCallback(onError);
      if (cb) await cb(error, variables, context as TContext | undefined);
    },
  });
}