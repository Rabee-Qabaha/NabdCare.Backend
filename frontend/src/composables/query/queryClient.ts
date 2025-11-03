import {
  QueryClient,
  focusManager,
  onlineManager,
  QueryCache,
  type Query,
} from "@tanstack/vue-query";
import { reactive } from "vue";

/**
 * 🌐 Global Vue Query Client (SaaS-grade Configuration – 2025)
 * ------------------------------------------------------------
 * ✅ Smart cache & background revalidation
 * ✅ Auto refetch every 2m (only when active)
 * ✅ Idle-aware refetch suspension
 * ✅ Network awareness
 * ✅ Telemetry metrics for Dev insight
 * ✅ SWR behavior & auto invalidation
 */

export interface QueryMetrics {
  totalQueries: number;
  activeQueries: number;
  cacheHits: number;
  networkFetches: number;
  refetchCount: number;
  mutationCount: number;
  lastSyncTime: number | null;
  idleSince: number | null;
}

// 🧠 Reactive metrics (shared across app)
export const queryMetrics = reactive<QueryMetrics>({
  totalQueries: 0,
  activeQueries: 0,
  cacheHits: 0,
  networkFetches: 0,
  refetchCount: 0,
  mutationCount: 0,
  lastSyncTime: null,
  idleSince: null,
});

// ----------------------------------------------
// ⚙️ Query Cache Event Subscription
// ----------------------------------------------
const queryCache = new QueryCache();

queryCache.subscribe((event) => {
  const { type, query } = event;

  switch (type) {
    case "added":
      queryMetrics.totalQueries++;
      break;
    case "removed":
      queryMetrics.totalQueries = Math.max(0, queryMetrics.totalQueries - 1);
      break;
    case "updated":
      if (query.state.status === "success") {
        queryMetrics.networkFetches++;
        queryMetrics.lastSyncTime = Date.now();
      }
      break;
  }
});

// ----------------------------------------------
// ⚙️ Create Query Client
// ----------------------------------------------
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Caching strategy: balance between freshness and performance
      staleTime: 1000 * 60 * 5, // 5 minutes - data is fresh
      gcTime: 1000 * 60 * 10, // 10 minutes - keep in memory (was cacheTime)
      retry: 1, // Retry failed requests once
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000), // Exponential backoff

      // Don't automatically refetch on window focus for auth checks
      refetchOnWindowFocus: false,
      refetchOnReconnect: "always",
      refetchOnMount: "always",
    },

    mutations: {
      retry: 1,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
  },
});

// ----------------------------------------------
// 🧠 Smart Idle Tracker
// ----------------------------------------------
let idleTimer: number | null = null;
let isIdle = false;

function markUserActive() {
  if (isIdle) {
    isIdle = false;
    focusManager.setFocused(true);
    console.log("👋 User active → resuming background polling");
    queryMetrics.idleSince = null;
  }

  if (idleTimer) clearTimeout(idleTimer);
  idleTimer = window.setTimeout(
    () => {
      isIdle = true;
      focusManager.setFocused(false);
      queryMetrics.idleSince = Date.now();
      console.log("💤 User idle → pausing background polling");
    },
    1000 * 60 * 5
  );
}

["mousemove", "keydown", "mousedown", "touchstart"].forEach((e) =>
  window.addEventListener(e, markUserActive)
);

window.addEventListener("online", () => {
  onlineManager.setOnline(true);
  console.log("🌐 Online → revalidating data");
});

window.addEventListener("offline", () => {
  onlineManager.setOnline(false);
  console.log("⚠️ Offline → pausing queries");
});

markUserActive();

// ----------------------------------------------
// 📊 Dev Telemetry Console Log
// ----------------------------------------------
if (import.meta.env.DEV) {
  setInterval(() => {
    const allQueries = queryClient.getQueryCache().getAll();
    const active = (allQueries as Query[]).filter(
      (q) => q.state.fetchStatus === "fetching"
    ).length;

    queryMetrics.activeQueries = active;
    queryMetrics.refetchCount++;

    console.table({
      "🧩 Total Queries": queryMetrics.totalQueries,
      "⚡ Active": queryMetrics.activeQueries,
      "💾 Cache Hits": queryMetrics.cacheHits,
      "🌍 Network Fetches": queryMetrics.networkFetches,
      "🔁 Refetch Count": queryMetrics.refetchCount,
      "🧠 Mutations": queryMetrics.mutationCount,
      "🕒 Last Sync":
        queryMetrics.lastSyncTime &&
        new Date(queryMetrics.lastSyncTime).toLocaleTimeString(),
      "💤 Idle Since":
        queryMetrics.idleSince &&
        new Date(queryMetrics.idleSince).toLocaleTimeString(),
    });
  }, 1000 * 30);
}

// ============================================================
// 🧭 TODO: Add "System Health Panel" (Admin Dashboard Integration)
// ------------------------------------------------------------
// - Create component: src/components/system/SystemHealthPanel.vue
// - Display metrics from `queryMetrics`
// - Add it only for SuperAdmin users OR in Dev mode
// ============================================================
