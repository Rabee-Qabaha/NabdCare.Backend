import { paymentsApi } from '@/api/modules/payments';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type { CreatePaymentRequestDto } from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function usePaymentActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();
  const { handleErrorAndNotify } = useErrorHandler();

  const invalidate = (clinicId?: string) => {
    if (clinicId) {
      queryClient.invalidateQueries({ queryKey: ['payments', clinicId] });
    } else {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
    }
  };

  const createMutation = useMutation({
    mutationFn: (data: CreatePaymentRequestDto) => paymentsApi.create(data),
    onSuccess: (data) => {
      toast.success('Payment recorded successfully');
      invalidate(data.clinicId);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const updateMutation = useMutation({
    mutationFn: (data: { id: string; dto: any }) => paymentsApi.update(data.id, data.dto),
    onSuccess: (data) => {
      toast.success('Payment updated');
      invalidate(data.clinicId);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => paymentsApi.delete(id),
    onSuccess: () => {
      toast.success('Payment deleted');
      invalidate();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    // Raw mutations
    createMutation,
    updateMutation,
    deleteMutation,

    // Convenience Aliases
    createPayment: createMutation.mutate,
    updatePayment: updateMutation.mutate,
    deletePayment: deleteMutation.mutate,

    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,

    // Permissions
    canRead: can('Payments.Read'),
    canCreate: can('Payments.Create'),
    canEdit: can('Payments.Edit'),
    canDelete: can('Payments.Delete'),
  };
}
