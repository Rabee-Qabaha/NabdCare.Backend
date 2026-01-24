// Path: src/composables/query/subscriptions/useSubscriptions.ts
import { subscriptionsApi } from '@/api/modules/subscriptions';
import { keepPreviousData, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

/* 🔹 Cache keys (Updated to accept 'string | null' for safety) */
export const subscriptionsKeys = {
  all: ['subscriptions'] as const,
  list: (params: any) => ['subscriptions', 'list', params] as const,
  byId: (id: string | null) => ['subscriptions', id] as const,
  clinic: (clinicId: string | null, params: any) =>
    ['subscriptions', 'clinic', clinicId, params] as const,
  active: (clinicId: string | null) => ['subscriptions', 'clinic', clinicId, 'active'] as const,
  plans: ['subscriptions', 'plans'] as const,
};

/* =========================================================================
   ✅ QUERIES (Read Operations)
   ========================================================================= */

/**
 * Fetch all subscriptions (SuperAdmin view)
 */
export function useAllSubscriptions(params: Ref<Record<string, any>> | Record<string, any> = {}) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.list(unref(params))),
    queryFn: () => subscriptionsApi.getAll(unref(params)),
    placeholderData: keepPreviousData,
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}

/**
 * Fetch subscriptions for a specific clinic
 * 🛠️ UPDATED: Accepts nullable ID (Ref<string | null>)
 */
export function useClinicSubscriptions(
  clinicId: Ref<string | null> | string | null,
  params: Ref<Record<string, any>> | Record<string, any> = {},
) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.clinic(unref(clinicId), unref(params))),
    queryFn: () => {
      const id = unref(clinicId);
      if (!id) throw new Error('Clinic ID is required');
      return subscriptionsApi.getByClinicId(id, unref(params));
    },
    // Only run if we actually have an ID string
    enabled: computed(() => !!unref(clinicId)),
    placeholderData: keepPreviousData,
  });
}

/**
 * Fetch the single ACTIVE subscription for a clinic (Summary Card)
 * 🛠️ UPDATED: Accepts nullable ID
 */
export function useActiveSubscription(clinicId: Ref<string | null> | string | null) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.active(unref(clinicId))),
    queryFn: async () => {
      const id = unref(clinicId);
      if (!id) return null;
      return subscriptionsApi.getActiveForClinic(id);
    },
    enabled: computed(() => !!unref(clinicId)),
    staleTime: 1000 * 60 * 2,
  });
}

/**
 * Fetch a single subscription by ID
 * 🛠️ UPDATED: Accepts nullable ID
 */
export function useSubscriptionById(id: Ref<string | null> | string | null) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.byId(unref(id))),
    queryFn: () => {
      const subId = unref(id);
      if (!subId) throw new Error('Subscription ID is required');
      return subscriptionsApi.getById(subId);
    },
    enabled: computed(() => !!unref(id)),
  });
}

export function useSubscriptionPlans() {
  return useQuery({
    queryKey: subscriptionsKeys.plans,
    queryFn: () => subscriptionsApi.getPlans(),
    staleTime: 1000 * 60 * 60, // 1 hour
    gcTime: 1000 * 60 * 60 * 24, // 24 hours
  });
}
