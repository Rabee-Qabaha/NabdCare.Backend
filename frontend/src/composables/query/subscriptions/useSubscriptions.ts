// src/composables/query/subscriptions/useSubscriptions.ts
import { useQueryWithToasts } from "@/composables/query/helpers/useQueryWithToasts";
import { useMutationWithInvalidate } from "@/composables/query/helpers/useMutationWithInvalidate";
import { subscriptionsApi } from "@/api/modules/subscriptions";
import type {
  CreateSubscriptionRequestDto,
  UpdateSubscriptionRequestDto,
} from "@/types/backend/index";
import type {
  SubscriptionStatus,
  SubscriptionType,
} from "@/types/backend/index";

/* 🔹 Query keys — for organized caching */
export const subscriptionsKeys = {
  all: ["subscriptions"] as const,
  byId: (id: string) => ["subscriptions", id] as const,
  clinic: (clinicId: string) => ["subscriptions", "clinic", clinicId] as const,
  active: (clinicId: string) =>
    ["subscriptions", "clinic", clinicId, "active"] as const,
};

/* ✅ Fetch all subscriptions (SuperAdmin) */
export function useAllSubscriptions(params?: Record<string, any>) {
  return useQueryWithToasts({
    queryKey: subscriptionsKeys.all,
    queryFn: () => subscriptionsApi.getAll(params),
    successMessage: "Subscriptions loaded successfully!",
    errorMessage: "Failed to load subscriptions.",
  });
}

/* ✅ Fetch subscriptions for a clinic */
export function useClinicSubscriptions(
  clinicId: string,
  params?: Record<string, any>
) {
  return useQueryWithToasts({
    queryKey: subscriptionsKeys.clinic(clinicId),
    queryFn: () => subscriptionsApi.getByClinicId(clinicId, params),
    enabled: !!clinicId,
    successMessage: "Clinic subscriptions loaded successfully!",
    errorMessage: "Failed to load clinic subscriptions.",
  });
}

/* ✅ Fetch active subscription for a clinic */
export function useActiveSubscription(clinicId: string) {
  return useQueryWithToasts({
    queryKey: subscriptionsKeys.active(clinicId),
    queryFn: () => subscriptionsApi.getActiveForClinic(clinicId),
    enabled: !!clinicId,
    successMessage: "Active subscription loaded successfully!",
    errorMessage: "Failed to load active subscription.",
  });
}

/* ✅ Fetch a single subscription by ID */
export function useSubscriptionById(id: string) {
  return useQueryWithToasts({
    queryKey: subscriptionsKeys.byId(id),
    queryFn: () => subscriptionsApi.getById(id),
    enabled: !!id,
    successMessage: "Subscription loaded successfully!",
    errorMessage: "Failed to load subscription details.",
  });
}

/* ✅ Mutations */

export function useCreateSubscription() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "create"],
    mutationFn: (payload: CreateSubscriptionRequestDto) =>
      subscriptionsApi.create(payload),
    invalidateKeys: [subscriptionsKeys.all],
    successMessage: "Subscription created successfully!",
    errorMessage: "Failed to create subscription.",
  });
}

export function useUpdateSubscription() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "update"],
    mutationFn: (data: { id: string; dto: UpdateSubscriptionRequestDto }) =>
      subscriptionsApi.update(data.id, data.dto),
    invalidateKeys: [
      (variables) => subscriptionsKeys.byId(variables.id),
      subscriptionsKeys.all,
    ],
    successMessage: "Subscription updated successfully!",
    errorMessage: "Failed to update subscription.",
  });
}

export function useChangeStatus() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "change-status"],
    mutationFn: (data: { id: string; newStatus: SubscriptionStatus }) =>
      subscriptionsApi.changeStatus(data.id, data.newStatus),
    invalidateKeys: [
      (variables) => subscriptionsKeys.byId(variables.id),
      subscriptionsKeys.all,
    ],
    successMessage: "Subscription status changed successfully!",
    errorMessage: "Failed to change subscription status.",
  });
}

export function useRenewSubscription() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "renew"],
    mutationFn: (data: { id: string; type: SubscriptionType }) =>
      subscriptionsApi.renew(data.id, data.type),
    invalidateKeys: [
      (variables) => subscriptionsKeys.byId(variables.id),
      subscriptionsKeys.all,
    ],
    successMessage: "Subscription renewed successfully!",
    errorMessage: "Failed to renew subscription.",
  });
}

export function useToggleAutoRenew() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "toggle-auto-renew"],
    mutationFn: (data: { id: string; enable: boolean }) =>
      subscriptionsApi.toggleAutoRenew(data.id, data.enable),
    invalidateKeys: [(variables) => subscriptionsKeys.byId(variables.id)],
    successMessage: "Auto-renew setting updated successfully!",
    errorMessage: "Failed to update auto-renew setting.",
  });
}

export function useCancelSubscription() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "cancel"],
    mutationFn: (id: string) => subscriptionsApi.cancel(id),
    invalidateKeys: [subscriptionsKeys.all],
    successMessage: "Subscription cancelled successfully!",
    errorMessage: "Failed to cancel subscription.",
  });
}

export function useHardDeleteSubscription() {
  return useMutationWithInvalidate({
    mutationKey: ["subscriptions", "hard-delete"],
    mutationFn: (id: string) => subscriptionsApi.hardDelete(id),
    invalidateKeys: [subscriptionsKeys.all],
    successMessage: "Subscription deleted successfully!",
    errorMessage: "Failed to delete subscription.",
  });
}
