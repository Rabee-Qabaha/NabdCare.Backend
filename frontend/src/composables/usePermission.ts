import { useAuthStore } from '@/stores/authStore';
import { permissionCache } from '@/utils/permissionCache';
import { computed } from 'vue';

export function usePermission() {
  const authStore = useAuthStore();

  function can(permission: string): boolean {
    return permissionCache.can(permission);
  }

  function canView(permission: string) {
    return computed(() => permissionCache.can(permission));
  }

  function canAny(permissions: string[]) {
    return permissions.some((p) => permissionCache.can(p));
  }

  return {
    can,
    canAny,
    canView,
    currentUser: computed(() => authStore.currentUser),
  };
}
