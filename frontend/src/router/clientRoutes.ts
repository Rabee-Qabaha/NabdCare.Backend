import type { RouteRecordRaw } from "vue-router";
import { UserRole } from "@/types/backend";

export const clientRoutes: RouteRecordRaw[] = [
  {
    path: "/client",
    component: () => import("@/layout/client/ClientLayout.vue"),
    meta: {
      requiresAuth: true,
      roles: [
        UserRole.ClinicAdmin,
        UserRole.Doctor,
        UserRole.Nurse,
        UserRole.Receptionist,
      ],
      public: false,
    },
    children: [
      {
        path: "",
        name: "dashboard",
        component: () => import("@/views/pages/client/Dashboard.vue"),
        meta: {
          title: "Client Dashboard",
          public: false,
        },
      },
      {
        path: "patients",
        name: "patients",
        component: () => import("@/views/pages/client/patiesnts/Patiesnts.vue"),
        meta: {
          title: "Patients",
          public: false,
        },
      },
      {
        path: "patients/:id",
        name: "patient-profile",
        component: () =>
          import("@/views/pages/client/patiesnts/PatiesntProfile.vue"),
        props: true,
        meta: {
          title: "Patient Profile",
          public: false,
        },
      },
      {
        path: "payments",
        name: "payments",
        component: () => import("@/views/pages/client/Payments/Payments.vue"),
        meta: {
          title: "Payments",
          public: false,
        },
      },
      {
        path: "users",
        name: "users",
        component: () => import("@/views/pages/client/Users/Users.vue"),
        meta: {
          title: "Users",
          public: false,
        },
      },
    ],
  },
];
