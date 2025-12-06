// src/composables/user/useUserDialogs.ts
import type { UserResponseDto } from '@/types/backend';
import { ref } from 'vue';

export function useUserDialogs() {
  const dialogs = ref({
    createEdit: false,
    permissions: false,
    resetPassword: false,
    deleteConfirm: false,
    permanentDelete: false,
    bulkAction: false,
  });

  const selectedUser = ref<UserResponseDto | null>(null);
  const currentBulkAction = ref<'activate' | 'deactivate' | 'delete' | null>(null);

  function openCreateDialog() {
    selectedUser.value = null;
    dialogs.value.createEdit = true;
  }

  function openEditDialog(user: UserResponseDto) {
    selectedUser.value = { ...user }; // Clone to avoid mutation
    dialogs.value.createEdit = true;
  }

  function openPermissionsDialog(user: UserResponseDto) {
    selectedUser.value = user;
    dialogs.value.permissions = true;
  }

  function openResetPasswordDialog(user: UserResponseDto) {
    selectedUser.value = user;
    dialogs.value.resetPassword = true;
  }

  function openDeleteDialog(user: UserResponseDto) {
    selectedUser.value = user;
    dialogs.value.deleteConfirm = true;
  }

  function openPermanentDeleteDialog(user: UserResponseDto) {
    selectedUser.value = user;
    dialogs.value.permanentDelete = true;
  }

  function openBulkActionDialog(action: 'activate' | 'deactivate' | 'delete') {
    currentBulkAction.value = action;
    dialogs.value.bulkAction = true;
  }

  function closeAll() {
    Object.keys(dialogs.value).forEach(
      (k) => (dialogs.value[k as keyof typeof dialogs.value] = false),
    );
    selectedUser.value = null;
    currentBulkAction.value = null;
  }

  return {
    dialogs,
    selectedUser,
    currentBulkAction,
    openCreateDialog,
    openEditDialog,
    openPermissionsDialog,
    openResetPasswordDialog,
    openDeleteDialog,
    openPermanentDeleteDialog,
    openBulkActionDialog,
    closeAll,
  };
}
