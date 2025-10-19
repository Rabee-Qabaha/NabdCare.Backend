// src/types/router.ts

import type { UserRole } from "@/types/backend";

export interface AppRouteMeta {
  requiresAuth?: boolean;
  roles?: UserRole[];
  public?: boolean;
  title?: string;
  [key: string]: unknown; // ✅ Allows extra keys, fixes TS errors
}
