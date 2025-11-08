// src/composables/query/authorization/useResourceAuthorization.ts
import { checkResourceAuthorization } from '@/api/modules/authorization';
import type { AuthorizationResultDto } from '@/types/backend';
import { useQuery, type UseQueryOptions } from '@tanstack/vue-query';
import { computed } from 'vue';

/**
 * Vue Query Composable for Resource Authorization Checks
 * Location: src/composables/query/authorization/useResourceAuthorization.ts
 *
 * Implements ABAC (Attribute-Based Access Control) with Vue Query
 *
 * Features:
 * - 5-minute caching (staleTime)
 * - Stale-while-revalidate pattern
 * - Automatic retry on failure
 * - Loading/error states
 * - Batch authorization checks
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

/**
 * Composable for single resource authorization check
 *
 * Usage:
 * ```typescript
 * const { allowed, isLoading, error, reason } = useResourceAuthorization(
 *   'user',
 *   userId,
 *   'edit'
 * );
 *
 * <Button :disabled="!allowed.value || isLoading.value">Edit</Button>
 * ```
 */
export function useResourceAuthorization(
  resourceType: string,
  resourceId: string,
  action: string,
  options?: UseQueryOptions<AuthorizationResultDto>,
) {
  const { data, isLoading, error, refetch } = useQuery<AuthorizationResultDto>({
    queryKey: ['authorization', resourceType, resourceId, action],
    queryFn: () => checkResourceAuthorization(resourceType, resourceId, action),
    staleTime: 5 * 60 * 1000, // 5 minutes - cached data is considered fresh
    gcTime: 10 * 60 * 1000, // 10 minutes - keep in memory for 10 min (was cacheTime)
    retry: 1, // Retry once on failure
    retryDelay: 1000, // Wait 1 second before retry
    ...options,
  });

  // Computed properties for easier usage
  const allowed = computed(() => data.value?.allowed ?? false);
  const reason = computed(() => data.value?.reason);
  const policy = computed(() => data.value?.policy);
  const isDenied = computed(() => !allowed.value && !isLoading.value);

  return {
    // Data
    result: data,
    allowed,
    reason,
    policy,
    isDenied,

    // States
    isLoading,
    error,

    // Methods
    refetch,
    recheck: () => refetch(), // Alias for clarity
  };
}

/**
 * Hook for checking multiple resources at once
 * Returns an object keyed by resourceId with authorization results
 *
 * Usage:
 * ```typescript
 * const { results, canAccess, isLoading } = useMultipleResourceAuthorizations(
 *   'user',
 *   [user1Id, user2Id, user3Id],
 *   'edit'
 * );
 *
 * <Button :disabled="!canAccess(user1Id)">Edit User 1</Button>
 * ```
 */
export function useMultipleResourceAuthorizations(
  resourceType: string,
  resourceIds: string[],
  action: string,
  options?: UseQueryOptions<Record<string, AuthorizationResultDto>>,
) {
  const { data, isLoading, error, refetch } = useQuery<Record<string, AuthorizationResultDto>>({
    queryKey: ['authorization:batch', resourceType, resourceIds, action],
    queryFn: async () => {
      const results = await Promise.all(
        resourceIds.map((id) =>
          checkResourceAuthorization(resourceType, id, action).catch((err) => ({
            allowed: false,
            reason: `Check failed: ${err.message}`,
            policy: '',
            resourceType,
            action,
          })),
        ),
      );

      return resourceIds.reduce(
        (acc, id, idx) => {
          acc[id] = results[idx];
          return acc;
        },
        {} as Record<string, AuthorizationResultDto>,
      );
    },
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: 1,
    retryDelay: 1000,
    enabled: resourceIds.length > 0, // Don't query if no IDs
    ...options,
  });

  // Helper to check single resource from batch
  const canAccess = (resourceId: string): boolean => data.value?.[resourceId]?.allowed ?? false;
  const getDenialReason = (resourceId: string): string | undefined =>
    data.value?.[resourceId]?.reason;

  return {
    // Data
    results: data,

    // States
    isLoading,
    error,

    // Methods
    canAccess,
    getDenialReason,
    refetch,
  };
}
