// src/utils/permissionCache.ts
import { useAuthStore } from '@/stores/authStore';
import { hasPermission } from '@/utils/permissions';

type CacheKey = string;

class PermissionCache {
  private cache = new Map<CacheKey, boolean>();
  private lastRoleId: string | null = null;

  /**
   * ✅ Checks permission with caching.
   * If permissions or role changed, clears cache automatically.
   */
  can(permission: string): boolean {
    const store = useAuthStore();

    // If role or user changed → clear cache
    if (store.currentUser?.roleId !== this.lastRoleId) {
      this.invalidate();
      this.lastRoleId = store.currentUser?.roleId || null;
    }

    // Cached?
    if (this.cache.has(permission)) {
      return this.cache.get(permission)!;
    }

    // Compute & store
    const result = hasPermission(permission);
    this.cache.set(permission, result);
    return result;
  }

  /**
   * ✅ Bulk check
   */
  canAny(permissions: string[]): boolean {
    return permissions.some((p) => this.can(p));
  }

  /**
   * ❌ Clears the cache manually
   */
  invalidate() {
    this.cache.clear();
  }
}

// Singleton instance
export const permissionCache = new PermissionCache();
