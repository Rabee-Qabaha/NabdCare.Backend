// src/utils/permissionCache.ts
import { useAuthStore } from '@/stores/authStore';

type PermissionKey = string;
type CacheKey = string;

class PermissionCache {
  private cache = new Map<CacheKey, boolean>();
  private lastRoleId: string | null = null;
  private lastVersion: string | null = null;

  /**
   * Returns true if permissions should be invalidated.
   * - Role changed
   * - Permissions version changed
   * - User logged out
   */
  private shouldInvalidate(): boolean {
    const store = useAuthStore();

    const roleChanged = store.currentUser?.roleId !== this.lastRoleId;
    const versionChanged = store.permissionsVersion !== this.lastVersion;

    const loggedOut = !store.currentUser && this.lastRoleId !== null;

    return roleChanged || versionChanged || loggedOut;
  }

  /** Sync internal tracking state with auth store */
  private syncState() {
    const store = useAuthStore();
    this.lastRoleId = store.currentUser?.roleId ?? null;
    this.lastVersion = store.permissionsVersion ?? null;
  }

  /** Clear cache if invalid or stale */
  private ensureFresh() {
    if (this.shouldInvalidate()) {
      this.invalidate();
      this.syncState();
    }
  }

  /** Core helper: compute a single permission value */
  private compute(permission: PermissionKey): boolean {
    const store = useAuthStore();

    // No permissions loaded yet
    if (!store.isPermissionsLoaded) return false;

    // SuperAdmin bypass
    if (store.isSuperAdmin) return true;

    // Normal permission check
    return store.permissions.includes(permission);
  }

  /** Check if user has a single permission */
  can(permission: PermissionKey): boolean {
    this.ensureFresh();

    if (this.cache.has(permission)) {
      return this.cache.get(permission)!;
    }

    const result = this.compute(permission);
    this.cache.set(permission, result);

    return result;
  }

  /** OR: checks if user has ANY of the given permissions */
  canAny(permissions: PermissionKey[]): boolean {
    return permissions.some((p) => this.can(p));
  }

  /** AND: checks if user has ALL of the given permissions */
  canAll(permissions: PermissionKey[]): boolean {
    return permissions.every((p) => this.can(p));
  }

  /** NOT: checks that user does NOT have the permission */
  canNot(permission: PermissionKey): boolean {
    return !this.can(permission);
  }

  /** Manual cache invalidation (used on logout) */
  invalidate() {
    this.cache.clear();
  }
}

export const permissionCache = new PermissionCache();
