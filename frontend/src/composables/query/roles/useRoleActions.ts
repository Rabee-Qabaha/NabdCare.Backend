// src/composables/query/roles/useRoleActions.ts
import { rolesApi } from '@/api/modules/roles';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type {
  CloneRoleRequestDto,
  CreateRoleRequestDto,
  UpdateRoleRequestDto,
} from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useRoleActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  // ✅ Import centralized error handler
  const { handleErrorAndNotify } = useErrorHandler();

  const ROLES_KEY = ['roles'];

  // -----------------------------------------
  // CREATE
  // -----------------------------------------
  const createMutation = useMutation({
    mutationFn: (data: CreateRoleRequestDto) => rolesApi.create(data),
    onSuccess: () => {
      toast.success('Role created successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
    // ✅ Use central handler
    onError: (err) => handleErrorAndNotify(err),
  });

  // -----------------------------------------
  // UPDATE
  // -----------------------------------------
  const updateMutation = useMutation({
    mutationFn: (payload: { roleId: string; data: UpdateRoleRequestDto }) =>
      rolesApi.update(payload.roleId, payload.data),
    onSuccess: () => {
      toast.success('Role updated successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -----------------------------------------
  // CLONE
  // -----------------------------------------
  const cloneMutation = useMutation({
    mutationFn: (payload: { id: string; data: CloneRoleRequestDto }) =>
      rolesApi.clone(payload.id, payload.data),
    onSuccess: () => {
      toast.success('Role cloned successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -----------------------------------------
  // DELETE (Soft)
  // -----------------------------------------
  const deleteMutation = useMutation({
    mutationFn: (roleId: string) => rolesApi.softDelete(roleId),
    onSuccess: () => {
      toast.success('Role moved to trash');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
    // ❌ Deleted: Optimistic Update logic (onMutate)
    onError: (err) => handleErrorAndNotify(err),
  });

  // -----------------------------------------
  // HARD DELETE (New)
  // -----------------------------------------
  const hardDeleteMutation = useMutation({
    mutationFn: (roleId: string) => rolesApi.hardDelete(roleId),
    onSuccess: () => {
      toast.success('Role permanently deleted');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -----------------------------------------
  // RESTORE
  // -----------------------------------------
  const restoreMutation = useMutation({
    mutationFn: (roleId: string) => rolesApi.restore(roleId),
    onSuccess: () => {
      toast.success('Role restored successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    // Actions
    createRole: createMutation.mutate,
    updateRole: (roleId: string, data: UpdateRoleRequestDto) =>
      updateMutation.mutate({ roleId, data }),
    cloneRole: (id: string, data: CloneRoleRequestDto) => cloneMutation.mutate({ id, data }),
    deleteRole: deleteMutation.mutate,
    hardDeleteRole: hardDeleteMutation.mutate,
    restoreRole: restoreMutation.mutate,

    // Mutations (Exposed for loading states)
    createMutation,
    updateMutation,
    cloneMutation,
    deleteMutation,
    hardDeleteMutation,
    restoreMutation,

    // Permissions
    canEditRole: can('Roles.Edit'),
    canDeleteRole: can('Roles.Delete'),
    canCloneRole: can('Roles.Clone'),
    canCreateRole: can('Roles.Create'),
    canRestoreRole: can('Roles.Restore'),
  };
}
