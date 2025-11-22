// src/composables/role/useRoleUIActions.ts
import type { RoleResponseDto } from '@/types/backend';
import { useConfirm } from 'primevue/useconfirm';
import { ref } from 'vue';

export function useRoleUIActions() {
  const confirm = useConfirm();
  const loadingRoleIds = ref<Set<string>>(new Set());

  const confirmDelete = (role: RoleResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Are you sure you want to delete "${role.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Cancel',
      acceptLabel: 'Delete',
      rejectProps: { outlined: true },
      acceptProps: { severity: 'danger' },
      accept: onConfirm,
    });
  };

  const confirmDuplicate = (role: RoleResponseDto, onConfirm: () => void) => {
    confirm.require({
      message: `Duplicate "${role.name}" role with all its permissions?`,
      header: 'Duplicate Role',
      icon: 'pi pi-copy',
      rejectLabel: 'Cancel',
      acceptLabel: 'Duplicate',
      acceptProps: { severity: 'info' },
      rejectProps: { outlined: true },
      accept: onConfirm,
    });
  };

  return {
    loadingRoleIds,
    confirmDelete,
    confirmDuplicate,
  };
}
