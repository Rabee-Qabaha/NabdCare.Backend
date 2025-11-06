import { createRouter, createWebHistory } from 'vue-router';
import type { RouteLocationNormalized, RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { getDefaultDashboardRoute } from '@/utils/navigation';
import { clientRoutes } from './clientRoutes';
import { superadminRoutes } from './superadminRoutes';
import { useAuthorizationGuard } from '@/composables/useAuthorizationGuard';

import Access from '@/views/pages/auth/Access.vue';
import NotFound from '@/views/pages/NotFound.vue';

/**
 * Main Router Configuration
 * Location: src/router/index.ts
 *
 * Features:
 * - Authentication checks
 * - Role-based access control (RBAC)
 * - Permission-based access control (PBAC)
 * - Attribute-based access control (ABAC)
 * - Route-level guards with multi-layer authorization
 *
 * Authorization Layers:
 * 1. Public routes: No auth required
 * 2. Authentication: User must be logged in
 * 3. RBAC: User must have required role
 * 4. PBAC: User must have required permission(s)
 * 5. Level check: System vs Clinic context
 * 6. ABAC: User must have access to specific resource
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

/**
 * 🧭 Create router instance
 */
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    ...clientRoutes,
    ...superadminRoutes,

    {
      path: '/',
      redirect: { name: 'login' },
    },
    {
      path: '/auth/login',
      name: 'login',
      component: () => import('@/views/pages/auth/Login.vue'),
      meta: { public: true, title: 'Login' },
    },
    {
      path: '/auth/access',
      name: 'accessDenied',
      component: Access,
      meta: { public: true, title: 'Access Denied' },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'notfound',
      component: NotFound,
      meta: { public: true, title: 'Not Found' },
    },
  ],
});

/**
 * ✅ Validate redirect URL against router routes
 * Prevents open redirect vulnerabilities (CWE-601)
 *
 * Checks:
 * - URL must be a string
 * - URL must start with /
 * - URL must not contain protocol (://)
 * - Route must exist in router
 * - Route must not be login or accessDenied (prevent loops)
 */
function isValidRedirectUrl(path: string): boolean {
  try {
    if (typeof path !== 'string') return false;
    if (!path.startsWith('/')) return false;
    if (path.includes('://') || path.startsWith('//')) return false;

    const resolved = router.resolve(path);
    if (!resolved) return false;
    if (resolved.name === 'login' || resolved.name === 'accessDenied') {
      return false;
    }

    return true;
  } catch {
    return false;
  }
}

/**
 * ✅ Check if user has role required for route (RBAC)
 */
function canAccessRouteByRole(
  to: RouteLocationNormalized,
  userRole: string | null,
  isSuperAdmin: boolean,
): boolean {
  const requiredRoles = (to.meta as any).roles as string[] | string | undefined;

  if (!requiredRoles) return true; // No role requirement

  const normalizedRequired = Array.isArray(requiredRoles) ? requiredRoles : [requiredRoles];

  // SuperAdmin can access everything
  if (isSuperAdmin) return true;

  // Check if user's role is in required roles
  return normalizedRequired.includes(userRole || '');
}

/**
 * ✅ Check if user has permission required for route (PBAC)
 */
function canAccessRouteByPermission(
  to: RouteLocationNormalized,
  authStore: ReturnType<typeof useAuthStore>,
): boolean {
  const requiredPermissions = (to.meta as any).permission || ((to.meta as any).permissions as any);

  if (!requiredPermissions) return true; // No permission requirement

  const normalizedRequired = Array.isArray(requiredPermissions)
    ? requiredPermissions
    : [requiredPermissions];

  // SuperAdmin can access everything
  if (authStore.isSuperAdmin) return true;

  // Check if user has any of the required permissions
  return normalizedRequired.some((p) => authStore.hasPermission(p));
}

/**
 * ✅ Check if route level matches user context
 * SuperAdmin routes should only be accessed by SuperAdmin
 * Clinic routes should only be accessed by clinic users
 */
function canAccessRouteByLevel(
  to: RouteLocationNormalized,
  authStore: ReturnType<typeof useAuthStore>,
): boolean {
  const level = (to.meta as any).level as 'system' | 'clinic' | undefined;

  if (!level) return true; // No level requirement

  if (level === 'system') {
    // System routes require SuperAdmin role
    return authStore.isSuperAdmin;
  }

  if (level === 'clinic') {
    // Clinic routes require clinic context
    return authStore.hasClinicContext;
  }

  return true;
}

/**
 * ✅ NEW: Check ABAC (Attribute-Based Access Control) for resource-specific routes
 * Used for detail/edit routes that access specific resources
 *
 * This adds a third layer of authorization:
 * 1. RBAC: Does user have the role?
 * 2. PBAC: Does user have the permission?
 * 3. ABAC: Can user access THIS SPECIFIC resource?
 *
 * Example:
 * - Route: /users/:id
 * - RBAC: User has "Admin" role ✅
 * - PBAC: User has "Users.View" permission ✅
 * - ABAC: User can access user#123 (in same clinic) ✅ → Allow
 * - ABAC: User cannot access user#456 (different clinic) ❌ → Deny
 */
async function canAccessRouteByABAC(
  to: RouteLocationNormalized,
  authStore: ReturnType<typeof useAuthStore>,
): Promise<boolean> {
  const abacResource = (to.meta as any).abacResource as string | null | undefined;
  const abacResourceIdParam = (to.meta as any).abacResourceIdParam as string | undefined;
  const abacAction = (to.meta as any).abacAction as string | undefined;

  // No ABAC check needed for this route
  if (!abacResource || !abacResourceIdParam) {
    return true;
  }

  // Extract resource ID from route params
  const resourceId = to.params[abacResourceIdParam] as string | undefined;
  if (!resourceId) {
    console.warn(`⚠️ Missing resource ID parameter: ${abacResourceIdParam}`);
    return false;
  }

  // Default action is 'view'
  const action = abacAction || 'view';

  try {
    // Use the authorization guard composable to check access
    // This will query the backend /authorization/check endpoint
    const { canAccess, isChecking } = useAuthorizationGuard(
      abacResource as any,
      resourceId,
      action as any,
    );

    // Wait for check to complete (with timeout to prevent infinite loops)
    let attempts = 0;
    const maxAttempts = 50; // 5 seconds max (50 * 100ms)

    while (isChecking.value && attempts < maxAttempts) {
      await new Promise((resolve) => setTimeout(resolve, 100));
      attempts++;
    }

    if (attempts >= maxAttempts) {
      console.warn(`⚠️ ABAC check timeout for ${abacResource}:${resourceId}:${action}`);
      return false;
    }

    const result = canAccess.value;

    if (!result) {
      console.warn(`🚫 ABAC check denied: ${abacResource}:${resourceId}:${action}`);
    }

    return result;
  } catch (err) {
    console.error(`❌ ABAC check error for ${abacResource}:${resourceId}:${action}`, err);
    // On error, deny access to be safe
    return false;
  }
}

/**
 * 🔒 Global route guard
 * Comprehensive auth + permission + role + ABAC checks
 */
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore();

  try {
    // ✅ 1. Set document title
    if (to.meta.title) {
      document.title = `${to.meta.title} - NabdCare`;
    }

    // ✅ 2. Ensure auth initialization
    if (!authStore.isInitialized) {
      console.log('⏳ Initializing auth...');
      await authStore.initAuth();
    }

    // ✅ 3. Wait until permissions are fully loaded
    if (!authStore.isPermissionsLoaded && authStore.isLoggedIn) {
      console.log('⏳ Waiting for permissions...');
      await authStore.waitForPermissions();
    }

    // ✅ 4. Allow all public routes
    if (to.meta.public) {
      // Prevent logged-in users from accessing login page again
      if (authStore.isLoggedIn && ['login', 'accessDenied'].includes(to.name as string)) {
        return next(getDefaultDashboardRoute());
      }

      return next();
    }

    // 🚫 5. Block unauthenticated users
    if (!authStore.isLoggedIn) {
      console.log('🔒 User not logged in - redirecting to login');
      return next({
        name: 'login',
        query: { redirect: to.fullPath },
      });
    }

    // ✅ 6. Check role-based access (RBAC)
    if (!canAccessRouteByRole(to, authStore.userRole, authStore.isSuperAdmin)) {
      console.warn(`🚫 User role '${authStore.userRole}' not allowed for route:`, to.path);
      return next({ name: 'accessDenied' });
    }

    // ✅ 7. Check route level (system vs clinic)
    if (!canAccessRouteByLevel(to, authStore)) {
      console.warn(`🚫 User context not allowed for route level:`, to.path);
      return next({ name: 'accessDenied' });
    }

    // ✅ 8. Check permission-based access (PBAC)
    if (!canAccessRouteByPermission(to, authStore)) {
      console.warn('🚫 Missing required permission for route:', to.path);
      return next({ name: 'accessDenied' });
    }

    // ✅ 9. Check ABAC (Attribute-Based Access Control) for resource-specific routes
    if (!(await canAccessRouteByABAC(to, authStore))) {
      console.warn('🚫 ABAC check failed for resource access:', to.path);
      return next({ name: 'accessDenied' });
    }

    // ✅ 10. Save last visited protected route (for redirect after login)
    if (!to.meta.public && !['login', 'accessDenied', 'notfound'].includes(to.name as string)) {
      try {
        localStorage.setItem('lastVisitedRoute', to.fullPath);
      } catch {
        console.warn('⚠️ Could not save last visited route to localStorage');
      }
    }

    next();
  } catch (err) {
    console.error('❌ Route guard error:', err);
    next({ name: 'accessDenied' });
  }
});

/**
 * ✅ After navigation - cleanup redirect params
 */
router.afterEach((to, from) => {
  // Clean URL if we just logged in from redirect
  if (from.name === 'login' && to.name !== 'login' && !to.query.redirect) {
    // Remove ?redirect= from URL
    if (typeof window !== 'undefined' && window.location.search) {
      window.history.replaceState({}, '', to.fullPath);
    }
  }
});

export default router;
