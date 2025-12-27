// src/composables/query/subscriptions/useSubscriptionActions.ts
import { subscriptionsApi } from '@/api/modules/subscriptions';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { subscriptionsKeys } from '@/composables/query/subscriptions/useSubscriptions';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type {
  CreateSubscriptionRequestDto,
  SubscriptionStatus,
  SubscriptionType,
  UpdateSubscriptionRequestDto,
} from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useSubscriptionActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();
  const { handleErrorAndNotify } = useErrorHandler();

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: subscriptionsKeys.all });
    // Also invalidate invoices since creating a subscription generates an invoice
    queryClient.invalidateQueries({ queryKey: ['invoices'] });
  };

  // --- MUTATIONS ---

  const createMutation = useMutation({
    mutationFn: (data: CreateSubscriptionRequestDto) => subscriptionsApi.create(data),
    onSuccess: () => {
      toast.success('Subscription created successfully');
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const updateMutation = useMutation({
    mutationFn: (payload: { id: string; data: UpdateSubscriptionRequestDto }) =>
      subscriptionsApi.update(payload.id, payload.data),
    onSuccess: () => {
      toast.success('Subscription updated successfully');
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const renewMutation = useMutation({
    mutationFn: (payload: { id: string; type: SubscriptionType }) =>
      subscriptionsApi.renew(payload.id, payload.type),
    onSuccess: () => {
      toast.success('Subscription renewed successfully');
      invalidateAll();
    },
    onError: (err: any) => {
      if (err.response?.status === 409) {
        toast.warn('A renewal is already queued for this subscription.');
      } else {
        handleErrorAndNotify(err);
      }
    },
  });

  const toggleAutoRenewMutation = useMutation({
    mutationFn: (payload: { id: string; enable: boolean }) =>
      subscriptionsApi.toggleAutoRenew(payload.id, payload.enable),
    onSuccess: (_, variables) => {
      const status = variables.enable ? 'enabled' : 'disabled';
      toast.success(`Auto-renew ${status}`);
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const changeStatusMutation = useMutation({
    mutationFn: (payload: { id: string; status: SubscriptionStatus }) =>
      subscriptionsApi.changeStatus(payload.id, payload.status),
    onSuccess: () => {
      toast.success('Status updated successfully');
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => subscriptionsApi.cancel(id),
    onSuccess: () => {
      toast.success('Subscription cancelled');
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const hardDeleteMutation = useMutation({
    mutationFn: (id: string) => subscriptionsApi.hardDelete(id),
    onSuccess: () => {
      toast.success('Subscription permanently deleted');
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    // Methods (Quick helpers)
    createSubscription: createMutation.mutate,
    updateSubscription: (id: string, data: UpdateSubscriptionRequestDto) =>
      updateMutation.mutate({ id, data }),
    renewSubscription: (id: string, type: SubscriptionType) => renewMutation.mutate({ id, type }),
    toggleAutoRenew: (id: string, enable: boolean) =>
      toggleAutoRenewMutation.mutate({ id, enable }),
    changeStatus: (id: string, status: SubscriptionStatus) =>
      changeStatusMutation.mutate({ id, status }),
    cancelSubscription: cancelMutation.mutate,
    hardDeleteSubscription: hardDeleteMutation.mutate,

    // ✅ Mutation Objects (Expose these so components can check .isPending)
    createSubscriptionMutation: createMutation, // <--- ADDED THIS LINE
    renewMutation,
    toggleAutoRenewMutation,
    cancelMutation,
    hardDeleteMutation,
    updateMutation,

    // Permissions
    canCreate: can('Subscriptions.Create'),
    canEdit: can('Subscriptions.Edit'),
    canPurchaseAddons: can('Subscriptions.PurchaseAddons'),
    canRenew: can('Subscriptions.Renew'),
    canToggleAutoRenew: can('Subscriptions.ToggleAutoRenew'),
    canCancel: can('Subscriptions.Cancel'),
    canHardDelete: can('Subscriptions.HardDelete'),
    canViewAll: can('Subscriptions.ViewAll'),
  };
}
