import { computed } from "vue";
import { queryMetrics } from "./queryClient";

/**
 * useQueryMetrics()
 * -----------------
 * Reactive hook to monitor Vue Query metrics in real time.
 * Can be used inside dashboards or admin monitoring pages.
 */
export function useQueryMetrics() {
  return {
    metrics: queryMetrics,
    formatted: computed(() => ({
      total: queryMetrics.totalQueries,
      active: queryMetrics.activeQueries,
      cacheHits: queryMetrics.cacheHits,
      networkFetches: queryMetrics.networkFetches,
      refetches: queryMetrics.refetchCount,
      lastSync: queryMetrics.lastSyncTime
        ? new Date(queryMetrics.lastSyncTime).toLocaleTimeString()
        : "—",
      idleSince: queryMetrics.idleSince
        ? new Date(queryMetrics.idleSince).toLocaleTimeString()
        : "Active",
    })),
  };
}
