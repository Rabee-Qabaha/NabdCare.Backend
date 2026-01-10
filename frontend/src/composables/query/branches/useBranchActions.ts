import { branchesApi } from '@/api/modules/branches';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type { CreateBranchRequestDto, UpdateBranchRequestDto } from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useBranchActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();
  const { handleErrorAndNotify } = useErrorHandler();

  const invalidate = (clinicId?: string) => {
    queryClient.invalidateQueries({ queryKey: ['branches'] });
    if (clinicId) {
      // Also refresh clinic details to update "BranchCount" if shown elsewhere
      queryClient.invalidateQueries({ queryKey: ['clinics', 'id', clinicId] });
    }
  };

  const createMutation = useMutation({
    mutationFn: (data: CreateBranchRequestDto) => branchesApi.create(data),
    onSuccess: (data) => {
      toast.success('Branch created successfully');
      invalidate(data.clinicId);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const updateMutation = useMutation({
    mutationFn: (data: { id: string; dto: UpdateBranchRequestDto }) =>
      branchesApi.update(data.id, data.dto),
    onSuccess: (data) => {
      toast.success('Branch updated');
      invalidate(data.clinicId);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const toggleStatusMutation = useMutation({
    mutationFn: (id: string) => branchesApi.toggleStatus(id),
    onSuccess: (data) => {
      const status = data.isActive ? 'activated' : 'deactivated';
      toast.success(`Branch ${status}`);
      invalidate(data.clinicId);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => branchesApi.delete(id),
    onSuccess: () => {
      toast.success('Branch deleted');
      invalidate();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    createMutation,
    updateMutation,
    deleteMutation,
    toggleStatusMutation,

    // Permissions
    canCreate: can('Branches.Create'),
    canEdit: can('Branches.Edit'),
    canDelete: can('Branches.Delete'),
    canToggleStatus: can('Branches.ToggleStatus'),
    canSetMain: can('Branches.SetMain'),
  };
}
