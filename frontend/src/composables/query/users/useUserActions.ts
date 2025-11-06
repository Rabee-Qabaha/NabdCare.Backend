import { userApi } from '@/api/modules/users';
import type { ChangePasswordRequestDto, ResetPasswordRequestDto } from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

/**
 * User Action Mutations
 * Location: src/composables/query/users/useUserActions.ts
 *
 * Purpose:
 * Centralized location for all user action mutations
 * Includes: password changes, status updates, deletions
 *
 * ✅ Benefits:
 * - Single source of truth for user mutations
 * - Automatic cache invalidation
 * - Consistent error handling
 * - Easy to test and mock
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

// ========================================
// PASSWORD MUTATIONS
// ========================================

/**
 * 🔒 useChangePassword
 * Handles password change for the currently logged-in user (self-service)
 *
 * Usage:
 * ```typescript
 * const mutation = useChangePassword();
 * await mutation.mutateAsync({
 *   id: userId,
 *   data: { oldPassword: "...", newPassword: "..." }
 * });
 * ```
 */
export function useChangePassword() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'change-password'],
    mutationFn: async (payload: { id: string; data: ChangePasswordRequestDto }) => {
      const { id, data } = payload;
      await userApi.changePassword(id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to change password:', error);
    },
  });
}

/**
 * 🧩 useResetPassword
 * Allows admins to reset another user's password
 *
 * Usage:
 * ```typescript
 * const mutation = useResetPassword();
 * await mutation.mutateAsync({
 *   id: userId,
 *   data: { newPassword: "..." }
 * });
 * ```
 */
export function useResetPassword() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'reset-password'],
    mutationFn: async (payload: { id: string; data: ResetPasswordRequestDto }) => {
      const { id, data } = payload;
      await userApi.resetPassword(id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to reset password:', error);
    },
  });
}

// ========================================
// STATUS MUTATIONS
// ========================================

/**
 * ✅ useActivateUser
 * Activate an inactive user account
 *
 * Usage:
 * ```typescript
 * const mutation = useActivateUser();
 * await mutation.mutateAsync(userId);
 * ```
 */
export function useActivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'activate'],
    mutationFn: (id: string) => userApi.activate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to activate user:', error);
    },
  });
}

/**
 * ⛔ useDeactivateUser
 * Deactivate an active user account (prevents login)
 *
 * Usage:
 * ```typescript
 * const mutation = useDeactivateUser();
 * await mutation.mutateAsync(userId);
 * ```
 */
export function useDeactivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'deactivate'],
    mutationFn: (id: string) => userApi.deactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to deactivate user:', error);
    },
  });
}

// ========================================
// DELETE MUTATIONS
// ========================================

/**
 * 🗑️ useSoftDeleteUser
 * Soft delete a user (recoverable, marked as deleted)
 *
 * Usage:
 * ```typescript
 * const mutation = useSoftDeleteUser();
 * await mutation.mutateAsync(userId);
 * ```
 */
export function useSoftDeleteUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'soft-delete'],
    mutationFn: (id: string) => userApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to delete user:', error);
    },
  });
}

/**
 * 💀 useHardDeleteUser
 * Hard delete a user (permanent, cannot be recovered)
 *
 * Usage:
 * ```typescript
 * const mutation = useHardDeleteUser();
 * await mutation.mutateAsync(userId);
 * ```
 */
export function useHardDeleteUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'hard-delete'],
    mutationFn: (id: string) => userApi.hardDelete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to hard delete user:', error);
    },
  });
}

// ========================================
// UPDATE MUTATIONS
// ========================================

/**
 * 👤 useUpdateUserRole
 * Update a user's role
 *
 * Usage:
 * ```typescript
 * const mutation = useUpdateUserRole();
 * await mutation.mutateAsync({ id: userId, roleId: newRoleId });
 * ```
 */
export function useUpdateUserRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'update-role'],
    mutationFn: async (payload: { id: string; roleId: string }) => {
      const { id, roleId } = payload;
      await userApi.updateRole(id, roleId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: any) => {
      console.error('❌ Failed to update user role:', error);
    },
  });
}
/** * ♻️ useRestoreUser
 * Restore a soft-deleted user
 *
 * Usage:
 * ```typescript
 * const mutation = useRestoreUser();
 * await mutation.mutateAsync(userId);
 * ```
 */
export function useRestoreUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ['user', 'restore'],
    mutationFn: (id: string) => userApi.restoreUser(id),
    onSuccess: () => {
      +(
        // ✅ Refresh active list
        (+queryClient.invalidateQueries({
          queryKey: ['users', 'infinite', '', false],
        }))
      );
      +(
        // ✅ Refresh deleted list
        (+queryClient.invalidateQueries({
          queryKey: ['users', 'infinite', '', true],
        }))
      );
    },
    onError: (error: any) => {
      console.error('❌ Failed to restore user:', error);
    },
  });
}
