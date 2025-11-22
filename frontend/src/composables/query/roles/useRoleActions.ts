import { rolesApi } from '@/api/modules/roles';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type { CreateRoleRequestDto, RoleResponseDto, UpdateRoleRequestDto } from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useRoleActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();

  const ROLES_KEY = ['roles'];

  // CREATE ROLE
  const createMutation = useMutation({
    mutationFn: (data: CreateRoleRequestDto) => rolesApi.create(data),

    onSuccess: () => {
      toast.success('Role created successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },

    onError: () => toast.error('Failed to create role'),
  });

  // UPDATE ROLE
  const updateMutation = useMutation({
    mutationFn: (payload: { roleId: string; data: UpdateRoleRequestDto }) =>
      rolesApi.update(payload.roleId, payload.data),

    onSuccess: () => {
      toast.success('Role updated successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },

    onError: () => toast.error('Failed to update role'),
  });

  // DELETE ROLE (optimistic)
  const deleteMutation = useMutation({
    mutationFn: (roleId: string) => rolesApi.softDelete(roleId),

    onMutate: async (roleId: string) => {
      await queryClient.cancelQueries({ queryKey: ROLES_KEY });

      const prev = queryClient.getQueryData<RoleResponseDto[]>(ROLES_KEY);

      if (prev) {
        queryClient.setQueryData<RoleResponseDto[]>(
          ROLES_KEY,
          prev.map((r) => (r.id === roleId ? { ...r, isDeleted: true } : r)),
        );
      }

      return { prev };
    },

    onError: (_err, _id, ctx) => {
      if (ctx?.prev) queryClient.setQueryData(ROLES_KEY, ctx.prev);
      toast.error('Failed to delete role');
    },

    onSuccess: () => {
      toast.success('Role deleted successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },
  });

  // RESTORE
  const restoreMutation = useMutation({
    mutationFn: (roleId: string) => rolesApi.restore(roleId),

    onSuccess: () => {
      toast.success('Role restored successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },

    onError: () => toast.error('Failed to restore role'),
  });

  // CLONE ROLE
  const cloneMutation = useMutation({
    mutationFn: (payload: {
      roleId: string;
      clinicId?: string | null;
      newRoleName?: string | null;
      description?: string | null;
      copyPermissions?: boolean;
    }) =>
      rolesApi.clone(payload.roleId, {
        clinicId: payload.clinicId ?? null,
        newRoleName: payload.newRoleName ?? null,
        description: payload.description ?? null,
        copyPermissions: payload.copyPermissions ?? true,
      }),

    onSuccess: () => {
      toast.success('Role cloned successfully');
      queryClient.invalidateQueries({ queryKey: ROLES_KEY });
    },

    onError: () => toast.error('Failed to clone role'),
  });

  return {
    // actions
    createRole: createMutation.mutate,
    updateRole: (roleId: string, data: UpdateRoleRequestDto) =>
      updateMutation.mutate({ roleId, data }),
    deleteRole: deleteMutation.mutate,
    restoreRole: restoreMutation.mutate,
    cloneRole: cloneMutation.mutate,

    // expose mutations
    createMutation,
    updateMutation,
    deleteMutation,
    restoreMutation,
    cloneMutation,

    // permissions
    canEditRole: can('Roles.Edit'),
    canDeleteRole: can('Roles.Delete'),
    canCloneRole: can('Roles.Clone'),
    canCreateRole: can('Roles.Create'),
    canRestoreRole: can('Roles.Restore'),
  };
}
