import { Patients } from '@/types/backend/constants/patients';
import type { RouteRecordRaw } from 'vue-router';

/**
 * Client Routes
 * Location: src/router/clientRoutes.ts
 *
 * Routes for clinic users (non-SuperAdmin)
 * Typically accessed from /client path
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

export const clientRoutes: RouteRecordRaw[] = [
  {
    path: '/client',
    component: () => import('@/layout/client/ClientLayout.vue'),
    meta: {
      requiresAuth: true,
      title: 'Client Area',
      public: false,
      // 🧠 Clinic users only
      level: 'clinic',
    },
    children: [
      {
        path: '',
        name: 'client-dashboard',
        component: () => import('@/views/pages/client/Dashboard.vue'),
        meta: {
          title: 'Client Dashboard',
          permission: Patients.view,
          // ✅ Dashboard doesn't need individual resource checks
          abacResource: null,
        },
      },

      // ==========================================
      // FUTURE: Uncomment when implementing
      // ==========================================

      // {
      //   path: "patients",
      //   name: "client-patients",
      //   component: () => import("@/views/pages/client/patients/Patients.vue"),
      //   meta: {
      //     title: "Patients",
      //     permission: Patients.view,
      //     // ✅ List views don't need individual resource ABAC checks
      //     // ABAC happens at component level (buttons, actions)
      //     abacResource: null,
      //   },
      // },

      // {
      //   path: "patients/:id",
      //   name: "client-patient-detail",
      //   component: () => import("@/views/pages/client/patients/PatientDetail.vue"),
      //   meta: {
      //     title: "Patient Details",
      //     permission: Patients.view,
      //     // ✅ Detail route: check authorization for specific patient
      //     abacResource: "patient",
      //     abacResourceIdParam: "id",
      //     abacAction: "view", // Can only view this patient's details
      //   },
      // },

      // {
      //   path: "patients/:id/edit",
      //   name: "client-patient-edit",
      //   component: () => import("@/views/pages/client/patients/PatientEdit.vue"),
      //   meta: {
      //     title: "Edit Patient",
      //     permission: Patients.edit,
      //     // ✅ Edit route: check edit authorization for specific patient
      //     abacResource: "patient",
      //     abacResourceIdParam: "id",
      //     abacAction: "edit",
      //   },
      // },

      // {
      //   path: "payments",
      //   name: "client-payments",
      //   component: () => import("@/views/pages/client/payments/Payments.vue"),
      //   meta: {
      //     title: "Payments",
      //     permission: Payments.view,
      //     abacResource: null,
      //   },
      // },

      // {
      //   path: "users",
      //   name: "client-users",
      //   component: () => import("@/views/pages/client/users/Users.vue"),
      //   meta: {
      //     title: "Clinic Users",
      //     permission: Users.view,
      //     abacResource: null,
      //   },
      // },
    ],
  },
];
