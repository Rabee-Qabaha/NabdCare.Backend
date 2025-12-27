// src/composables/query/clinics/useClinicActions.ts
import { clinicsApi } from '@/api/modules/clinics';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import type { CreateClinicRequestDto, UpdateClinicRequestDto } from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useClinicActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();
  const { handleErrorAndNotify } = useErrorHandler();

  const CLINICS_KEY = ['clinics'];

  // 🆕 CREATE
  const createClinicMutation = useMutation({
    mutationFn: (data: CreateClinicRequestDto) => clinicsApi.create(data),
    onSuccess: () => {
      toast.success('Clinic created successfully');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 📝 UPDATE
  const updateClinicMutation = useMutation({
    mutationFn: (payload: { id: string; data: UpdateClinicRequestDto }) =>
      clinicsApi.update(payload.id, payload.data),
    onSuccess: (_, variables) => {
      toast.success('Clinic updated successfully');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
      queryClient.invalidateQueries({ queryKey: ['clinic', variables.id] });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 🗑 SOFT DELETE
  const softDeleteMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.delete(id),
    onSuccess: () => {
      toast.success('Clinic moved to trash');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 💀 HARD DELETE
  const hardDeleteMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.hardDelete(id),
    onSuccess: () => {
      toast.success('Clinic permanently deleted');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // ♻ RESTORE
  const restoreMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.restore(id),
    onSuccess: () => {
      toast.success('Clinic restored successfully');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // ▶️ ACTIVATE
  const activateMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.activate(id),
    onSuccess: () => {
      toast.success('Clinic activated');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // ⏸️ SUSPEND
  const suspendMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.suspend(id),
    onSuccess: () => {
      toast.warn('Clinic suspended');
      queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  return {
    createClinicMutation,
    updateClinicMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
    activateMutation,
    suspendMutation,

    // Permissions
    canCreate: can('Clinics.Create'),
    canEdit: can('Clinics.Edit'),
    canDelete: can('Clinics.Delete'),
    canHardDelete: can('Clinics.HardDelete'),
    canManageStatus: can('Clinics.ManageStatus'),
    canRestore: can('Clinics.Restore'),
  };
}
