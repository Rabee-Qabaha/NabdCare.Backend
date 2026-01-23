// src/router/index.ts
import { usePermission } from '@/composables/usePermission';
import { useAuthStore } from '@/stores/authStore';
import { getDefaultDashboardRoute } from '@/utils/navigation';
import type { RouteLocationNormalized } from 'vue-router';
import { createRouter, createWebHistory } from 'vue-router';

import { clientRoutes } from './clientRoutes';
import { superadminRoutes } from './superadminRoutes';

import Access from '@/views/pages/auth/Access.vue';
import NotFound from '@/views/pages/NotFound.vue';

// --------------------------------------------------
// Router Setup
// --------------------------------------------------
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    ...clientRoutes,
    ...superadminRoutes,

    { path: '/', redirect: { name: 'login' } },

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

// --------------------------------------------------
// Role-based Access (RBAC)
// --------------------------------------------------
function canAccessRouteByRole(
  to: RouteLocationNormalized,
  userRole: string | null,
  isSuperAdmin: boolean,
) {
  const requiredRoles = (to.meta as any).roles;

  if (!requiredRoles) return true;
  if (isSuperAdmin) return true;

  const normalized = Array.isArray(requiredRoles) ? requiredRoles : [requiredRoles];

  return normalized.includes(userRole || '');
}

// --------------------------------------------------
// Permission-based Access (PBAC)
// NEW — Uses PermissionRegistry via usePermission()
// --------------------------------------------------
function canAccessRouteByPermission(
  to: RouteLocationNormalized,
  authStore: ReturnType<typeof useAuthStore>,
) {
  const { can } = usePermission();

  const required = (to.meta as any).permissions || (to.meta as any).permission; // backward compatibility

  if (!required) return true;

  const normalized = Array.isArray(required) ? required : [required];

  if (authStore.isSuperAdmin) return true;

  return normalized.some((perm) => can(perm));
}

// --------------------------------------------------
// System/Clinic Level Access
// --------------------------------------------------
function canAccessRouteByLevel(
  to: RouteLocationNormalized,
  authStore: ReturnType<typeof useAuthStore>,
) {
  const level = (to.meta as any).level as 'system' | 'clinic' | undefined;

  if (!level) return true;

  if (level === 'system') return authStore.isSuperAdmin;
  if (level === 'clinic') return authStore.hasClinicContext;

  return true;
}

// --------------------------------------------------
// ABAC — TEMPORARILY DISABLED
// Always returns true
// --------------------------------------------------
async function canAccessRouteByABAC(): Promise<boolean> {
  // TODO: Rebuild ABAC later
  return true;
}

// --------------------------------------------------
// Global Before Guard
// --------------------------------------------------
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore();

  try {
    // Set document title
    if (to.meta.title) {
      document.title = `${to.meta.title} - NabdCare`;
    }

    // Ensure auth initialized
    if (!authStore.isInitialized) {
      await authStore.initAuth();
    }

    // Public route
    if (to.meta.public) {
      if (authStore.isLoggedIn && ['login', 'accessDenied'].includes(to.name as string)) {
        return next(getDefaultDashboardRoute());
      }
      return next();
    }

    // Not logged in
    if (!authStore.isLoggedIn) {
      return next({
        name: 'login',
        query: { redirect: to.fullPath },
      });
    }

    // RBAC
    if (!canAccessRouteByRole(to, authStore.userRole, authStore.isSuperAdmin)) {
      return next({ name: 'accessDenied' });
    }

    // System/Clinic Level
    if (!canAccessRouteByLevel(to, authStore)) {
      return next({ name: 'accessDenied' });
    }

    // PBAC
    if (!canAccessRouteByPermission(to, authStore)) {
      return next({ name: 'accessDenied' });
    }

    // ABAC (always true for now)
    if (!(await canAccessRouteByABAC())) {
      return next({ name: 'accessDenied' });
    }

    // Save last route
    if (!to.meta.public && !['login', 'accessDenied', 'notfound'].includes(to.name as string)) {
      localStorage.setItem('lastVisitedRoute', to.fullPath);
    }

    next();
  } catch (err) {
    console.error('❌ Route Guard Error:', err);
    next({ name: 'accessDenied' });
  }
});

export default router;
