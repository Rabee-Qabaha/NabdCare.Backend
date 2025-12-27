// src/composables/clinic/useClinicDialogs.ts
import type { ClinicResponseDto } from '@/types/backend';
import { ref } from 'vue';

export function useClinicDialogs() {
  const dialogs = ref({
    createEdit: false,
    deleteConfirm: false,
    details: false,
    permanentDelete: false,
  });

  const selectedClinic = ref<ClinicResponseDto | null>(null);
  const dialogMode = ref<'create' | 'edit'>('create');

  function openCreateDialog() {
    selectedClinic.value = null;
    dialogMode.value = 'create';
    dialogs.value.createEdit = true;
  }

  function openEditDialog(clinic: ClinicResponseDto) {
    selectedClinic.value = { ...clinic };
    dialogMode.value = 'edit';
    dialogs.value.createEdit = true;
  }

  function openDetailsDialog(clinic: ClinicResponseDto) {
    selectedClinic.value = clinic;
    dialogs.value.details = true;
  }

  function openDeleteDialog(clinic: ClinicResponseDto) {
    selectedClinic.value = clinic;
    dialogs.value.deleteConfirm = true;
  }

  function openPermanentDeleteDialog(clinic: ClinicResponseDto) {
    selectedClinic.value = clinic;
    dialogs.value.permanentDelete = true;
  }

  function closeAll() {
    dialogs.value.createEdit = false;
    dialogs.value.deleteConfirm = false;
    dialogs.value.details = false;
    dialogs.value.permanentDelete = false;
    selectedClinic.value = null;
  }

  return {
    dialogs,
    selectedClinic,
    dialogMode,
    openCreateDialog,
    openEditDialog,
    openDetailsDialog,
    openDeleteDialog,
    openPermanentDeleteDialog,
    closeAll,
  };
}
