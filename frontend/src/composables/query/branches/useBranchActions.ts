// src/composables/query/branches/useBranchActions.ts
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

  const createMutation = useMutation({
    mutationFn: (data: CreateBranchRequestDto) => branchesApi.create(data),
    onSuccess: (_, vars) => {
      toast.success('Branch created successfully');
      queryClient.invalidateQueries({ queryKey: ['branches', 'list', vars.clinicId] });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const updateMutation = useMutation({
    mutationFn: (data: { id: string; dto: UpdateBranchRequestDto }) =>
      branchesApi.update(data.id, data.dto),
    onSuccess: (_, vars) => {
      toast.success('Branch updated');
      queryClient.invalidateQueries({ queryKey: ['branches'] }); // Broad invalidation to update lists
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => branchesApi.delete(id),
    onSuccess: () => {
      toast.success('Branch deleted');
      queryClient.invalidateQueries({ queryKey: ['branches'] });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    createMutation,
    updateMutation,
    deleteMutation,
    canCreate: can('Branches.Create'),
    canEdit: can('Branches.Edit'),
    canDelete: can('Branches.Delete'),
  };
}
