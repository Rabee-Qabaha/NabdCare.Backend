// src/composables/user/useUserUIActions.ts
import type { UserResponseDto } from '@/types/backend';
import { useConfirm } from 'primevue/useconfirm';
import { ref } from 'vue';
import { useUserActions } from '../query/users/useUserActions';

export function useUserUIActions() {
  const confirm = useConfirm();
  const loadingUserIds = ref<Set<string>>(new Set());

  const {
    activateMutation,
    deactivateMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
  } = useUserActions();

  const toggleUserStatus = async (user: UserResponseDto) => {
    loadingUserIds.value.add(user.id);
    try {
      if (user.isActive) {
        await deactivateMutation.mutateAsync(user.id);
        // Success toast handled in useUserActions
      } else {
        await activateMutation.mutateAsync(user.id);
        // Success toast handled in useUserActions
      }
    } catch (err: any) {
      // Error toast handled by Global Interceptor, but keep specific fallback if needed
      console.error('Status toggle failed', err);
    } finally {
      loadingUserIds.value.delete(user.id);
    }
  };

  const confirmSoftDelete = (user: UserResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Are you sure you want to move ${user.fullName} to the trash?`,
      header: 'Confirm Soft Delete',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Cancel',
      acceptLabel: 'Move to Trash',
      rejectProps: { outlined: true, severity: 'secondary' },
      acceptProps: { severity: 'danger' },
      accept: onConfirm,
    });
  };

  const confirmHardDelete = (user: UserResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `This action is IRREVERSIBLE. Permanently delete ${user.fullName}?`,
      header: '⚠️ Permanent Deletion',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Cancel',
      acceptLabel: 'Delete Forever',
      rejectProps: { outlined: true, severity: 'secondary' },
      acceptProps: { severity: 'danger' },
      accept: onConfirm,
    });
  };

  const confirmRestore = (user: UserResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Restore user ${user.fullName}?`,
      header: 'Restore User',
      icon: 'pi pi-refresh',
      rejectLabel: 'Cancel',
      acceptLabel: 'Restore',
      acceptIcon: 'pi pi-undo',
      acceptProps: { severity: 'success' },
      rejectProps: { outlined: true, severity: 'secondary' },
      accept: onConfirm,
    });
  };

  return {
    loadingUserIds,
    toggleUserStatus,
    confirmSoftDelete,
    confirmHardDelete,
    confirmRestore,
    // Expose mutations if needed by parent
    activateMutation,
    deactivateMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
  };
}
