// src/router/superadminRoutes.ts
import type { RouteRecordRaw } from "vue-router";
import { UserRole } from "@/types/backend";
import type { AppRouteMeta } from "./router";

export const superadminRoutes: RouteRecordRaw[] = [
  {
    path: "/superadmin",
    component: () => import("@/layout/admin/AdminLayout.vue"),
    meta: {
      requiresAuth: true,
      roles: [UserRole.SuperAdmin],
      public: false,
      title: "Super Admin Dashboard",
    } as AppRouteMeta,
    children: [
      {
        path: "",
        name: "superadmin-dashboard",
        component: () => import("@/views/pages/admin/Dashboard.vue"),
        meta: { title: "Super Admin Dashboard", public: false } as AppRouteMeta,
      },
    ],
  },
];
