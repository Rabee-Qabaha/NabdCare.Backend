import { PermissionRegistry } from '@/config/permissionsRegistry';
import type { RouteRecordRaw } from 'vue-router';

/**
 * SuperAdmin Routes
 * Location: src/router/superadminRoutes.ts
 */

export const superadminRoutes: RouteRecordRaw[] = [
  {
    path: '/superadmin',
    component: () => import('@/layout/admin/AdminLayout.vue'),
    meta: {
      requiresAuth: true,
      public: false,
      title: 'Super Admin Area',
      level: 'system',
    },
    children: [
      {
        path: '',
        name: 'superadmin-dashboard',
        component: () => import('@/views/admin/Dashboard.vue'),
        meta: {
          title: 'Dashboard',
          permission: PermissionRegistry.Reports.viewDashboard,
          abacResource: null,
        },
      },
      {
        path: 'users',
        name: 'superadmin-users',
        component: () => import('@/views/admin/users/UsersManagement.vue'),
        meta: {
          title: 'Users Management',
          permission: PermissionRegistry.Users.view,
          abacResource: null,
        },
      },
      {
        path: 'roles',
        name: 'superadmin-roles', // Fixed typo (removed comma)
        component: () => import('@/views/admin/roles/RolesManagement.vue'),
        meta: {
          title: 'Manage Roles',
          permission: PermissionRegistry.Roles.viewAll,
          abacResource: null,
        },
      },
      // --------------------------------------------------------
      // 🏥 CLINICS MANAGEMENT
      // --------------------------------------------------------
      {
        path: 'clinics',
        name: 'superadmin-clinics',
        component: () => import('@/views/admin/clinics/ClinicsManagement.vue'),
        meta: {
          title: 'Manage Clinics',
          permission: PermissionRegistry.Clinics.viewAll,
          abacResource: null,
        },
      },

      // ✅ NEW: Clinic Dashboard (Detail View)
      // This matches the "Layout" component we built (ClinicDashboardLayout.vue)
      {
        path: 'clinics/:id',
        component: () => import('@/views/admin/clinics/dashboard/ClinicDashboardLayout.vue'),
        props: true, // Passes :id as a prop to the Layout
        meta: {
          title: 'Clinic Dashboard',
          permission: PermissionRegistry.Clinics.viewAll, // Or specific 'view' permission
        },
        children: [
          // 1. Overview Tab (Default)
          {
            path: '',
            name: 'clinic-overview',
            component: () => import('@/views/admin/clinics/dashboard/tabs/ClinicOverview.vue'),
            props: true, // Passes 'stats' prop if forwarded
            meta: {
              title: 'Clinic Overview',
            },
          },
          // 2. Subscription Tab
          {
            path: 'subscription',
            name: 'clinic-subscription',
            component: () => import('@/views/admin/clinics/dashboard/tabs/ClinicSubscription.vue'),
            meta: {
              title: 'Clinic Subscription',
            },
          },
          // 3. Invoices Tab
          {
            path: 'invoices',
            name: 'clinic-invoices',
            component: () => import('@/views/admin/clinics/dashboard/tabs/ClinicInvoices.vue'),
            meta: {
              title: 'Clinic Invoices',
            },
          },
          // 3. Branches Tab
          {
            path: 'branches',
            name: 'clinic-branches',
            component: () => import('@/views/admin/clinics/dashboard/tabs/ClinicBranches.vue'),
            meta: {
              title: 'Clinic Branches',
            },
          },
          // 4. Payments Tab
          {
            path: 'payments',
            name: 'clinic-payments',
            component: () => import('@/views/admin/clinics/dashboard/tabs/ClinicPayments.vue'),
            meta: {
              title: 'Clinic Payments',
            },
          },
          // Future Tabs: Users...
        ],
      },
    ],
  },
];
