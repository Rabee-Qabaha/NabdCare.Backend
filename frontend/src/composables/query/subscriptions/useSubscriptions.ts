import { subscriptionsApi } from '@/api/modules/subscriptions';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { useToastService } from '@/composables/useToastService';
import type {
  CreateSubscriptionRequestDto,
  SubscriptionStatus,
  SubscriptionType,
  UpdateSubscriptionRequestDto,
} from '@/types/backend/index';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

/* 🔹 Cache keys */
export const subscriptionsKeys = {
  all: ['subscriptions'] as const,
  list: (params: any) => ['subscriptions', 'list', params] as const,
  byId: (id: string) => ['subscriptions', id] as const,
  clinic: (clinicId: string, params: any) => ['subscriptions', 'clinic', clinicId, params] as const,
  active: (clinicId: string) => ['subscriptions', 'clinic', clinicId, 'active'] as const,
};

/* ✅ Queries */

export function useAllSubscriptions(params: Ref<Record<string, any>> | Record<string, any> = {}) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.list(unref(params))),
    queryFn: () => subscriptionsApi.getAll(unref(params)),
    placeholderData: keepPreviousData, // Prevents table flickering
    staleTime: 1000 * 60 * 5,
  });
}

export function useClinicSubscriptions(
  clinicId: Ref<string> | string,
  params: Ref<Record<string, any>> | Record<string, any> = {},
) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.clinic(unref(clinicId), unref(params))),
    queryFn: () => subscriptionsApi.getByClinicId(unref(clinicId), unref(params)),
    enabled: computed(() => !!unref(clinicId)),
    placeholderData: keepPreviousData,
  });
}

export function useActiveSubscription(clinicId: Ref<string> | string) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.active(unref(clinicId))),
    queryFn: () => subscriptionsApi.getActiveForClinic(unref(clinicId)),
    enabled: computed(() => !!unref(clinicId)),
  });
}

export function useSubscriptionById(id: Ref<string> | string) {
  return useQuery({
    queryKey: computed(() => subscriptionsKeys.byId(unref(id))),
    queryFn: () => subscriptionsApi.getById(unref(id)),
    enabled: computed(() => !!unref(id)),
  });
}

/* ✅ Mutations (Grouped in Composable) */

export function useSubscriptionActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { handleErrorAndNotify } = useErrorHandler();

  // Helper to invalidate related queries
  const invalidateCommon = async (id?: string, clinicId?: string) => {
    await queryClient.invalidateQueries({ queryKey: subscriptionsKeys.all });
    if (id) await queryClient.invalidateQueries({ queryKey: subscriptionsKeys.byId(id) });
    if (clinicId) {
      await queryClient.invalidateQueries({
        queryKey: ['subscriptions', 'clinic', clinicId], // Invalidate list
      });
      await queryClient.invalidateQueries({ queryKey: subscriptionsKeys.active(clinicId) });
    }
  };

  // 1. CREATE
  const createMutation = useMutation({
    mutationFn: (payload: CreateSubscriptionRequestDto) => subscriptionsApi.create(payload),
    onSuccess: (_, variables) => {
      toast.success('Subscription created successfully!');
      invalidateCommon(undefined, variables.clinicId);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 2. UPDATE
  const updateMutation = useMutation({
    mutationFn: (data: { id: string; dto: UpdateSubscriptionRequestDto }) =>
      subscriptionsApi.update(data.id, data.dto),
    onSuccess: (_, variables) => {
      toast.success('Subscription updated successfully!');
      invalidateCommon(variables.id);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 3. CHANGE STATUS
  const changeStatusMutation = useMutation({
    mutationFn: (data: { id: string; newStatus: SubscriptionStatus }) =>
      subscriptionsApi.changeStatus(data.id, data.newStatus),
    onSuccess: (_, variables) => {
      toast.success('Subscription status updated!');
      invalidateCommon(variables.id);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 4. RENEW
  const renewMutation = useMutation({
    mutationFn: (data: { id: string; type: SubscriptionType }) =>
      subscriptionsApi.renew(data.id, data.type),
    onSuccess: (_, variables) => {
      toast.success('Subscription renewed successfully!');
      invalidateCommon(variables.id);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 5. TOGGLE AUTO-RENEW
  const toggleAutoRenewMutation = useMutation({
    mutationFn: (data: { id: string; enable: boolean }) =>
      subscriptionsApi.toggleAutoRenew(data.id, data.enable),
    onSuccess: (_, variables) => {
      toast.success(`Auto-renew ${variables.enable ? 'enabled' : 'disabled'}`);
      queryClient.invalidateQueries({ queryKey: subscriptionsKeys.byId(variables.id) });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 6. CANCEL (Soft Delete)
  const cancelMutation = useMutation({
    mutationFn: (id: string) => subscriptionsApi.cancel(id),
    onSuccess: (_, id) => {
      toast.success('Subscription cancelled');
      invalidateCommon(id);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 7. HARD DELETE
  const hardDeleteMutation = useMutation({
    mutationFn: (id: string) => subscriptionsApi.hardDelete(id),
    onSuccess: () => {
      toast.success('Subscription deleted permanently');
      invalidateCommon();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    // Mutations
    createMutation,
    updateMutation,
    changeStatusMutation,
    renewMutation,
    toggleAutoRenewMutation,
    cancelMutation,
    hardDeleteMutation,

    // Convenience Methods
    createSubscription: createMutation.mutate,
    updateSubscription: (id: string, dto: UpdateSubscriptionRequestDto) =>
      updateMutation.mutate({ id, dto }),
    changeStatus: (id: string, newStatus: SubscriptionStatus) =>
      changeStatusMutation.mutate({ id, newStatus }),
    renewSubscription: (id: string, type: SubscriptionType) => renewMutation.mutate({ id, type }),
    toggleAutoRenew: (id: string, enable: boolean) =>
      toggleAutoRenewMutation.mutate({ id, enable }),
    cancelSubscription: cancelMutation.mutate,
    hardDeleteSubscription: hardDeleteMutation.mutate,
  };
}
