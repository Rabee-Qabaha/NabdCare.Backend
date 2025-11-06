// src/utils/permissions.ts
import { useAuthStore } from '@/stores/authStore';

export function hasPermission(permissionName: string): boolean {
  const store = useAuthStore();

  if (!store.isPermissionsLoaded) {
    // Wait for permissions to load before evaluating
    return false;
  }

  if (store.isSuperAdmin) return true;
  return store.permissions.includes(permissionName);
}

export function hasAnyPermission(...permissions: string[]): boolean {
  const store = useAuthStore();

  if (!store.isPermissionsLoaded) {
    return false;
  }

  if (store.isSuperAdmin) return true;
  return permissions.some((p) => store.permissions.includes(p));
}
