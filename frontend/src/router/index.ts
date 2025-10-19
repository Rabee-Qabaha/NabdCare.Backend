import {
  createRouter,
  createWebHistory,
  type RouteRecordRaw,
} from "vue-router";
import { useAuthStore } from "@/stores/authStore";
import { clientRoutes } from "./clientRoutes";
import { superadminRoutes } from "./superadminRoutes";
import { UserRole } from "@/types/backend";

const Login = () => import("@/views/pages/auth/Login.vue");
const AccessDenied = () => import("@/views/pages/auth/Access.vue");
const NotFound = () => import("@/views/pages/NotFound.vue");

const routes: RouteRecordRaw[] = [
  // 🔑 Root redirect
  {
    path: "/",
    redirect: () => {
      const authStore = useAuthStore();
      if (!authStore.isLoggedIn) return { name: "login" };
      return authStore.role === UserRole.SuperAdmin
        ? { name: "superadmin-dashboard" }
        : { name: "dashboard" };
    },
    meta: {
      public: true,
    },
  },

  {
    path: "/auth/login",
    name: "login",
    component: Login,
    meta: {
      public: true,
      title: "Login",
    },
  },

  {
    path: "/auth/access",
    name: "accessDenied",
    component: AccessDenied,
    meta: {
      public: true,
      title: "Access Denied",
    },
  },

  // Spread routes
  ...clientRoutes,
  ...superadminRoutes,

  {
    path: "/:pathMatch(.*)*",
    name: "notfound",
    component: NotFound,
    meta: {
      public: true,
      title: "Not Found",
    },
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: () => ({ top: 0 }),
});

// ✅ Auth Guard with proper typing
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  const meta = to.meta; // ✅ No need to cast anymore!

  console.log("🚦 Navigating to:", to.fullPath);
  console.log("📜 Route meta:", meta);
  console.log("👤 Current user:", authStore.currentUser);
  console.log("🎭 Normalized role:", authStore.role);
  console.log("🔑 Logged in:", authStore.isLoggedIn);

  // Set page title
  if (meta.title) {
    document.title = `${meta.title} - NabdCare`;
  }

  // Allow public routes
  if (meta.public) {
    return next();
  }

  // Check authentication
  if (!authStore.isLoggedIn) {
    console.log("❌ User not logged in → redirect to login");

    // Only add redirect param if not already on login page
    if (to.path !== "/auth/login") {
      return next({
        name: "login",
        query: { redirect: to.fullPath },
      });
    }

    return next({ name: "login" });
  }

  // Check role-based access
  const routeRoles = meta.roles;
  const userRole = authStore.role;

  if (routeRoles && (!userRole || !routeRoles.includes(userRole))) {
    console.log(
      `❌ Access denied! User role: ${userRole}, Allowed roles: ${routeRoles}`
    );
    return next({ name: "accessDenied" });
  }

  console.log("✅ Access granted");
  next();
});

export default router;
