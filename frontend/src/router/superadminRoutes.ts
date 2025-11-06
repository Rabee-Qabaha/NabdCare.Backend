import type { RouteRecordRaw } from 'vue-router';
import { PermissionRegistry } from '@/config/permissionsRegistry';

/**
 * SuperAdmin Routes
 * Location: src/router/superadminRoutes.ts
 *
 * Routes for SuperAdmin users only
 * Typically accessed from /superadmin path
 *
 * Features:
 * - System-level access (level: "system")
 * - Permission-based access control (RBAC)
 * - Resource-level access control (ABAC) for detail routes
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

export const superadminRoutes: RouteRecordRaw[] = [
  {
    path: '/superadmin',
    component: () => import('@/layout/admin/AdminLayout.vue'),
    meta: {
      requiresAuth: true,
      public: false,
      title: 'Super Admin Area',
      // 🧠 System-level routes (SuperAdmin only)
      level: 'system',
    },
    children: [
      {
        path: '',
        name: 'superadmin-dashboard',
        component: () => import('@/views/pages/admin/Dashboard.vue'),
        meta: {
          title: 'Dashboard',
          permission: PermissionRegistry.Reports.viewDashboard,
          // ✅ Dashboard doesn't need individual resource checks
          abacResource: null,
        },
      },

      {
        path: 'users',
        name: 'superadmin-users',
        component: () => import('@/views/pages/admin/Users.vue'),
        meta: {
          title: 'Manage Users',
          permission: PermissionRegistry.Users.view,
          // ✅ List view: ABAC happens at component level for action buttons
          abacResource: null,
        },
      },

      // ==========================================
      // FUTURE: Uncomment when implementing
      // ==========================================

      // {
      //   path: "users/:id",
      //   name: "superadmin-user-detail",
      //   component: () => import("@/views/pages/admin/UserDetail.vue"),
      //   meta: {
      //     title: "User Details",
      //     permission: PermissionRegistry.Users.view,
      //     // ✅ Detail route: check authorization for specific user
      //     abacResource: "user",
      //     abacResourceIdParam: "id",
      //     abacAction: "view",
      //   },
      // },

      // {
      //   path: "users/:id/edit",
      //   name: "superadmin-user-edit",
      //   component: () => import("@/views/pages/admin/UserEdit.vue"),
      //   meta: {
      //     title: "Edit User",
      //     permission: PermissionRegistry.Users.edit,
      //     // ✅ Edit route: check edit authorization for specific user
      //     abacResource: "user",
      //     abacResourceIdParam: "id",
      //     abacAction: "edit",
      //   },
      // },

      // {
      //   path: "clinics",
      //   name: "superadmin-clinics",
      //   component: () => import("@/views/pages/admin/Clinics.vue"),
      //   meta: {
      //     title: "Manage Clinics",
      //     permission: PermissionRegistry.Clinics.viewAll,
      //     abacResource: null,
      //   },
      // },

      // {
      //   path: "clinics/:id/edit",
      //   name: "superadmin-clinic-edit",
      //   component: () => import("@/views/pages/admin/ClinicEdit.vue"),
      //   meta: {
      //     title: "Edit Clinic",
      //     permission: PermissionRegistry.Clinics.edit,
      //     abacResource: "clinic",
      //     abacResourceIdParam: "id",
      //     abacAction: "edit",
      //   },
      // },

      // {
      //   path: "roles",
      //   name: "superadmin-roles",
      //   component: () => import("@/views/pages/admin/Roles.vue"),
      //   meta: {
      //     title: "Manage Roles",
      //     permission: PermissionRegistry.Roles.viewAll,
      //     abacResource: null,
      //   },
      // },

      // {
      //   path: "subscriptions",
      //   name: "superadmin-subscriptions",
      //   component: () => import("@/views/pages/admin/Subscriptions.vue"),
      //   meta: {
      //     title: "Manage Subscriptions",
      //     permission: PermissionRegistry.Subscriptions.viewAll,
      //     abacResource: null,
      //   },
      // },

      // {
      //   path: "subscriptions/:id",
      //   name: "superadmin-subscription-detail",
      //   component: () => import("@/views/pages/admin/SubscriptionDetail.vue"),
      //   meta: {
      //     title: "Subscription Details",
      //     permission: PermissionRegistry.Subscriptions.view,
      //     abacResource: "subscription",
      //     abacResourceIdParam: "id",
      //     abacAction: "view",
      //   },
      // },
    ],
  },
];
