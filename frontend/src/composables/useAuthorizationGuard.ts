import { computed } from "vue";
import { useResourceAuthorization } from "@/composables/query/authorization/useResourceAuthorization";
import type { AuthorizationAction, ResourceType } from "@/types/authorization";

/**
 * Authorization Guard Composable
 * Location: src/composables/useAuthorizationGuard.ts
 *
 * Composable for protecting routes/components with ABAC checks
 * Combines permission checks with resource authorization
 *
 * Usage in route guards:
 * ```typescript
 * const { canAccess, isChecking } = useAuthorizationGuard('user', userId, 'edit');
 * if (!canAccess.value) {
 *   router.push('/access-denied');
 * }
 * ```
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

export function useAuthorizationGuard(
  resourceType: ResourceType,
  resourceId: string,
  action: AuthorizationAction
) {
  const { allowed, isLoading, error, reason, refetch } =
    useResourceAuthorization(resourceType, resourceId, action);

  // Computed properties
  const canAccess = computed(() => allowed.value && !error.value);
  const isChecking = computed(() => isLoading.value);
  const isDenied = computed(() => !allowed.value && !isLoading.value);
  const denialMessage = computed(() => {
    if (!isDenied.value) return null;
    if (reason.value) return reason.value;
    if (error.value)
      return `Authorization check failed: ${error.value.message}`;
    return "Access denied";
  });

  return {
    // States
    canAccess,
    isChecking,
    isDenied,
    denialMessage,
    error,

    // Methods
    checkAgain: refetch,
  };
}

/**
 * Quick authorization check for UI conditional rendering
 *
 * Usage in components:
 * ```typescript
 * const canEdit = useCanAccess('user', userId, 'edit');
 *
 * <Button v-if="canEdit.value" @click="editUser">Edit</Button>
 * ```
 */
export function useCanAccess(
  resourceType: ResourceType,
  resourceId: string,
  action: AuthorizationAction
) {
  const { allowed } = useResourceAuthorization(
    resourceType,
    resourceId,
    action
  );
  return allowed;
}
