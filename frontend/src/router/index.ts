// // src/router.ts
// import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
// import { useAuthStore } from '@/stores/authStore';
// import AppLayout from '@/layout/AppLayout.vue';
//
// // --------------------
// // Routes
// // --------------------
// const routes: RouteRecordRaw[] = [
//     {
//         path: '/',
//         component: AppLayout,
//         children: [
//             {
//                 path: '/',
//                 name: 'dashboard',
//                 component: () => import('../views/Dashboard.vue')
//             },
//             {
//                 path: '/pages/patients',
//                 name: 'patients',
//                 component: () => import('../views/pages/patiesnts/Patiesnts.vue')
//             },
//             {
//                 path: '/pages/patients/:id',
//                 name: 'patient-profile',
//                 component: () => import('../views/pages/patiesnts/PatiesntProfile.vue'),
//                 props: true
//             },
//             {
//                 path: '/pages/payments',
//                 name: 'payments',
//                 component: () => import('../views/pages/Payments/Payments.vue')
//             },
//             {
//                 path: '/pages/users',
//                 name: 'users',
//                 component: () => import('../views/pages/Users/Users.vue'),
//                 props: true
//             }
//         ]
//     },
//     {
//         path: '/pages/notfound',
//         name: 'notfound',
//         component: () => import('../views/pages/NotFound.vue')
//     },
//     {
//         path: '/auth/login',
//         name: 'login',
//         component: () => import('../views/pages/auth/Login.vue')
//     },
//     {
//         path: '/auth/access',
//         name: 'accessDenied',
//         component: () => import('../views/pages/auth/Access.vue')
//     },
//     {
//         path: '/auth/error',
//         name: 'error',
//         component: () => import('../views/pages/auth/Error.vue')
//     },
//     {
//         path: '/:pathMatch(.*)*',
//         redirect: '/pages/notfound'
//     }
// ];
//
// // --------------------
// // Router Instance
// // --------------------
// const router = createRouter({
//     history: createWebHistory(),
//     routes
// });
//
// // --------------------
// // Navigation Guard
// // --------------------
// router.beforeEach(async (to) => {
//     const authStore = useAuthStore();
//     const publicPages = ['/auth/login', '/auth/error', '/auth/access'];
//     const authRequired = !publicPages.includes(to.path);
//
//     // Wait for auth initialization if needed
//     if (!authStore.isInitialized) {
//         await new Promise((resolve) => {
//             const unwatch = authStore.$subscribe(() => {
//                 if (authStore.isInitialized) {
//                     unwatch();
//                     resolve(true);
//                 }
//             });
//         });
//     }
//
//     if (authRequired && !authStore.isLoggedIn) {
//         return '/auth/login';
//     }
// });
//
// export default router;

import {
  createRouter,
  createWebHistory,
  type RouteRecordRaw,
} from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import AppLayout from "@/layout/AppLayout.vue";

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    component: AppLayout,
    children: [
      {
        path: "",
        name: "dashboard",
        component: () => import("../views/pages/client/Dashboard.vue"),
      },
      // { path: 'patients', name: 'patients', component: () => import('@/pages/client/PatientsList.vue') },
      // { path: 'payments', name: 'payments', component: () => import('@/pages/client/Payments.vue') }
    ],
  },
  {
    path: "/admin",
    component: AppLayout,
    meta: { requiresAdmin: true },
    children: [
      {
        path: "dashboard",
        name: "admin-dashboard",
        component: () => import("../views/pages/admin/Dashboard.vue"),
      },
      // { path: 'users', name: 'admin-users', component: () => import('@/pages/admin/UsersList.vue') },
      // { path: 'clinics', name: 'admin-clinics', component: () => import('@/pages/admin/ClinicsList.vue') }
    ],
  },
  {
    path: "/auth/login",
    name: "login",
    component: () => import("../views/pages/auth/Login.vue"),
  },
  { path: "/:pathMatch(.*)*", redirect: "/auth/login" },
];

// ✅ Create router instance
const router = createRouter({
  history: createWebHistory(),
  routes,
});

// ✅ Navigation guard
router.beforeEach(async (to) => {
  const authStore = useAuthStore();

  if (!authStore.isInitialized) {
    authStore.tryRestoreSession();
  }

  const publicPages = ["/auth/login"];
  const authRequired = !publicPages.includes(to.path);

  if (authRequired && !authStore.isLoggedIn) return "/auth/login";
  if (to.meta.requiresAdmin && !authStore.isAdmin) return "/auth/access";
});

export default router;
