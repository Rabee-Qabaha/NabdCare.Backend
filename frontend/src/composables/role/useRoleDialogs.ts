// src/composables/role/useRoleDialogs.ts
import type { RoleResponseDto } from '@/types/backend';
import { ref } from 'vue';

export function useRoleDialogs() {
  const dialogs = ref({
    createEdit: false,
    permissions: false,
    details: false,
    deleteConfirm: false,
    permanentDelete: false,
  });

  const selectedRole = ref<RoleResponseDto | null>(null);
  const dialogMode = ref<'create' | 'edit' | 'clone'>('create');

  function openCreateDialog() {
    selectedRole.value = null;
    dialogMode.value = 'create';
    dialogs.value.createEdit = true;
  }

  function openEditDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogMode.value = 'edit';
    dialogs.value.createEdit = true;
  }

  function openCloneDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogMode.value = 'clone';
    dialogs.value.createEdit = true;
  }

  function openPermissionsDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.permissions = true;
  }

  function openDetailsDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.details = true;
  }

  function openDeleteDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.deleteConfirm = true;
  }

  function openPermanentDeleteDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.permanentDelete = true;
  }

  function closeAll() {
    dialogs.value.createEdit = false;
    dialogs.value.permissions = false;
    dialogs.value.details = false;
    dialogs.value.deleteConfirm = false;
    dialogs.value.permanentDelete = false;
    selectedRole.value = null;
  }

  return {
    dialogs,
    selectedRole,
    dialogMode,
    openCreateDialog,
    openEditDialog,
    openCloneDialog,
    openPermissionsDialog,
    openDetailsDialog,
    openDeleteDialog,
    openPermanentDeleteDialog,
    closeAll,
  };
}
