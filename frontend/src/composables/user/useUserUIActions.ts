// src/composables/user/useUserUIActions.ts
import { ref } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import type { UserResponseDto } from '../../types/backend/index';
import {
  useActivateUser,
  useDeactivateUser,
  useSoftDeleteUser,
  useHardDeleteUser,
  useRestoreUser,
} from '../query/users/useUserActions';

export function useUserUIActions() {
  const toast = useToast();
  const confirm = useConfirm();
  const loadingUserIds = ref<Set<string>>(new Set());

  const activateMutation = useActivateUser();
  const deactivateMutation = useDeactivateUser();
  const softDeleteMutation = useSoftDeleteUser();
  const hardDeleteMutation = useHardDeleteUser();
  const restoreMutation = useRestoreUser();

  const toggleUserStatus = async (user: UserResponseDto) => {
    loadingUserIds.value.add(user.id);
    try {
      if (user.isActive) {
        await deactivateMutation.mutateAsync(user.id);
        toast.add({
          severity: 'success',
          summary: 'Deactivated',
          detail: `${user.fullName} has been deactivated`,
          life: 3000,
        });
      } else {
        await activateMutation.mutateAsync(user.id);
        toast.add({
          severity: 'success',
          summary: 'Activated',
          detail: `${user.fullName} has been activated`,
          life: 3000,
        });
      }
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Failed to update user status',
        life: 3000,
      });
    } finally {
      loadingUserIds.value.delete(user.id);
    }
  };

  const confirmSoftDelete = (user: UserResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Are you sure you want to delete ${user.fullName}? This user will be soft-deleted and can be recovered.`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Cancel',
      acceptLabel: 'Delete',
      rejectProps: { outlined: true },
      acceptProps: { severity: 'danger' },
      accept: onConfirm,
    });
  };

  const confirmHardDelete = (user: UserResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Permanently delete ${user.fullName}? This action cannot be undone.`,
      header: '⚠️ Permanent Deletion',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Cancel',
      acceptLabel: 'Delete Permanently',
      rejectProps: { outlined: true },
      acceptProps: { severity: 'danger' },
      accept: onConfirm,
    });
  };

  const confirmRestore = (user: UserResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Restore user ${user.fullName} (${user.email})?`,
      header: 'Restore User',
      icon: 'pi pi-refresh',
      rejectLabel: 'Cancel',
      acceptLabel: 'Restore',
      acceptIcon: 'pi pi-undo',
      acceptProps: { severity: 'success' },
      rejectProps: { outlined: true },
      accept: onConfirm,
    });
  };

  return {
    loadingUserIds,
    toggleUserStatus,
    confirmSoftDelete,
    confirmHardDelete,
    confirmRestore,
    activateMutation,
    deactivateMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
  };
}