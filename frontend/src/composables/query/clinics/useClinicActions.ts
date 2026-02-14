// src/composables/query/clinics/useClinicActions.ts
import { clinicsApi } from '@/api/modules/clinics';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermission } from '@/composables/usePermission';
import { useToastService } from '@/composables/useToastService';
import { Clinics, type CreateClinicRequestDto, type UpdateClinicRequestDto } from '@/types/backend';
import { useMutation, useQueryClient } from '@tanstack/vue-query';

export function useClinicActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();
  const { can } = usePermission();
  const { handleErrorAndNotify } = useErrorHandler();

  const CLINICS_KEY = ['clinics'];

  // Helper to refresh specific clinic data
  const invalidateClinicData = (id: string) => {
    // 1. Refresh the list
    queryClient.invalidateQueries({ queryKey: CLINICS_KEY });
    // 2. Refresh the single clinic details (for Edit forms)
    queryClient.invalidateQueries({ queryKey: ['clinic', id] });
    // 3. ✅ Refresh the Dashboard Stats (New!)
    queryClient.invalidateQueries({ queryKey: ['clinic-stats', id] });
  };

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
      invalidateClinicData(variables.id); // ✅ Uses helper
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // 🗑 SOFT DELETE
  const softDeleteMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.delete(id),
    onSuccess: (_, id) => {
      toast.success('Clinic moved to trash');
      invalidateClinicData(id);
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
    onSuccess: (_, id) => {
      toast.success('Clinic restored successfully');
      invalidateClinicData(id);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // ▶️ ACTIVATE
  const activateMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.activate(id),
    onSuccess: (_, id) => {
      toast.success('Clinic activated');
      invalidateClinicData(id);
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // ⏸️ SUSPEND
  const suspendMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.suspend(id),
    onSuccess: (_, id) => {
      toast.warn('Clinic suspended');
      invalidateClinicData(id);
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
    canCreate: can(Clinics.create),
    canEdit: can(Clinics.edit),
    canDelete: can(Clinics.delete),
    canHardDelete: can(Clinics.hardDelete),
    canManageStatus: can(Clinics.manageStatus),
    canRestore: can(Clinics.restore),
  };
}
