// src/composables/query/users/useUserActions.ts
import { usersApi } from '@/api/modules/users';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type {
  ChangePasswordRequestDto,
  CreateUserRequestDto,
  ResetPasswordRequestDto,
  UpdateUserRequestDto,
} from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useUserActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();
  const { handleErrorAndNotify } = useErrorHandler();

  // Central Query Key
  const USERS_KEY = ['users'];

  // -------------------------------------------------------------------
  // 🆕 CREATE
  // -------------------------------------------------------------------
  const createUserMutation = useMutation({
    mutationFn: (data: CreateUserRequestDto) => usersApi.create(data),
    onSuccess: () => {
      toast.success('User created successfully');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // 📝 UPDATE
  // -------------------------------------------------------------------
  const updateUserMutation = useMutation({
    mutationFn: (payload: { id: string; data: UpdateUserRequestDto }) =>
      usersApi.update(payload.id, payload.data),
    onSuccess: (_, variables) => {
      toast.success('User updated successfully');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
      queryClient.invalidateQueries({ queryKey: ['user', variables.id] });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // 🗑 SOFT DELETE
  // -------------------------------------------------------------------
  const softDeleteMutation = useMutation({
    mutationFn: (id: string) => usersApi.delete(id),
    onSuccess: () => {
      toast.success('User moved to trash');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // 💀 HARD DELETE
  // -------------------------------------------------------------------
  const hardDeleteMutation = useMutation({
    mutationFn: (id: string) => usersApi.hardDelete(id),
    onSuccess: () => {
      toast.success('User permanently deleted');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // ♻ RESTORE
  // -------------------------------------------------------------------
  const restoreMutation = useMutation({
    mutationFn: (id: string) => usersApi.restore(id),
    onSuccess: () => {
      toast.success('User restored successfully');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // ✅ ACTIVATE
  // -------------------------------------------------------------------
  const activateMutation = useMutation({
    mutationFn: (id: string) => usersApi.activate(id),
    onSuccess: (_, id) => {
      toast.success('User activated');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
      queryClient.invalidateQueries({ queryKey: ['user', id] });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // ⛔ DEACTIVATE
  // -------------------------------------------------------------------
  const deactivateMutation = useMutation({
    mutationFn: (id: string) => usersApi.deactivate(id),
    onSuccess: (_, id) => {
      toast.success('User deactivated');
      queryClient.invalidateQueries({ queryKey: USERS_KEY });
      queryClient.invalidateQueries({ queryKey: ['user', id] });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // 🧩 RESET PASSWORD (Admin)
  // -------------------------------------------------------------------
  const resetPasswordMutation = useMutation({
    mutationFn: (payload: { id: string; data: ResetPasswordRequestDto }) =>
      usersApi.resetPassword(payload.id, payload.data),
    onSuccess: () => toast.success('Password reset successfully'),
    onError: (err) => handleErrorAndNotify(err),
  });

  // -------------------------------------------------------------------
  // 🔒 CHANGE PASSWORD (Self)
  // -------------------------------------------------------------------
  const changePasswordMutation = useMutation({
    mutationFn: (payload: { id: string; data: ChangePasswordRequestDto }) =>
      usersApi.changePassword(payload.id, payload.data),
    onSuccess: () => toast.success('Password updated successfully'),
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    // Mutations
    createUserMutation,
    updateUserMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
    activateMutation,
    deactivateMutation,
    resetPasswordMutation,
    changePasswordMutation,

    // Helper Methods (Direct Call)
    createUser: createUserMutation.mutate,
    updateUser: (id: string, data: UpdateUserRequestDto) => updateUserMutation.mutate({ id, data }),
    softDeleteUser: softDeleteMutation.mutate,
    hardDeleteUser: hardDeleteMutation.mutate,
    restoreUser: restoreMutation.mutate,

    // Permissions
    canCreate: can('Users.Create'),
    canEdit: can('Users.Edit'),
    canDelete: can('Users.Delete'),
    canHardDelete: can('Users.HardDelete'),
    canActivate: can('Users.Activate'),
    canResetPassword: can('Users.ResetPassword'),
  };
}
