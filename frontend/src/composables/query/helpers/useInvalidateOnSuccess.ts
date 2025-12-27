// Path: src/composables/query/helpers/useInvalidateOnSuccess.ts
import { useQueryClient } from '@tanstack/vue-query';

/**
 * Simple helper to invalidate a list of query keys after mutation success.
 */
export function useInvalidateOnSuccess(keys: readonly unknown[]) {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: keys });
}
