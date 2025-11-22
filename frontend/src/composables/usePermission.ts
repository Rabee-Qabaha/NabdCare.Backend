// src/composables/usePermission.ts
import type { PermissionCategoryKey } from '@/modules/permissions/permissionRegistry';
import { useAuthStore } from '@/stores/authStore';
import { permissionCache } from '@/utils/permissionCache';
import { computed } from 'vue';

/**
 * Central permission composable.
 * All permission checks should go through this.
 */
export function usePermission() {
  const authStore = useAuthStore();

  /**
   * Single permission check.
   * Uses cached evaluation for performance.
   */
  function can(permission: string): boolean {
    return permissionCache.can(permission);
  }

  /**
   * Check if the user has at least one permission from a list.
   */
  function canAny(permissions: string[]): boolean {
    return permissions.some((p) => permissionCache.can(p));
  }

  /**
   * Reactive permission check for templates.
   */
  function canView(permission: string) {
    return computed(() => permissionCache.can(permission));
  }

  /**
   * Helper: check all permissions for a specific category (e.g. "Users").
   */
  function canCategory(category: PermissionCategoryKey): boolean {
    const perms = authStore.permissions.filter((p) => p.startsWith(category + '.'));
    return perms.some((p) => permissionCache.can(p));
  }

  return {
    can,
    canAny,
    canView,
    canCategory,
    currentUser: computed(() => authStore.currentUser),
    permissionsVersion: computed(() => authStore.permissionsVersion),
  };
}
