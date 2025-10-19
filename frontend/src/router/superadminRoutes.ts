import type { RouteRecordRaw } from "vue-router";
import { UserRole } from "@/types/backend";

export const superadminRoutes: RouteRecordRaw[] = [
  {
    path: "/superadmin",
    component: () => import("@/layout/admin/AdminLayout.vue"),
    meta: {
      requiresAuth: true,
      roles: [UserRole.SuperAdmin],
      public: false,
      title: "Super Admin Dashboard",
    },
    children: [
      {
        path: "",
        name: "superadmin-dashboard",
        component: () => import("@/views/pages/admin/Dashboard.vue"),
        meta: {
          title: "Super Admin Dashboard",
          public: false,
        },
      },
      {
        path: "users",
        name: "superadmin-users",
        component: () => import("@/views/pages/admin/Users.vue"),
        meta: {
          title: "Manage Users",
          public: false,
        },
      },
    ],
  },
];
