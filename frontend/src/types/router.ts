// src/types/router.ts

import type { UserRole } from "@/types/backend";
import type { RouteMeta } from "vue-router";

// ✅ Extend Vue Router's RouteMeta interface
declare module "vue-router" {
  interface RouteMeta {
    requiresAuth?: boolean;
    roles?: UserRole[];
    public?: boolean;
    title?: string;
  }
}

// ✅ Export for convenience (optional)
export type AppRouteMeta = RouteMeta;
