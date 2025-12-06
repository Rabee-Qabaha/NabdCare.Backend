import { clinicsApi } from '@/api/modules/clinics';
import { useToastService } from '@/composables/useToastService';
import type {
  CreateClinicRequestDto,
  PaginationRequestDto,
  UpdateClinicRequestDto,
  UpdateClinicStatusDto,
} from '@/types/backend';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

/* 🔹 Cache keys */
export const clinicKeys = {
  all: ['clinics'] as const,
  list: (params: PaginationRequestDto) => ['clinics', 'list', params] as const,
  active: (params: PaginationRequestDto) => ['clinics', 'active', params] as const,
  byId: (id: string) => ['clinics', id] as const,
  me: ['clinics', 'me'] as const,
  search: (query: string, params?: PaginationRequestDto) =>
    ['clinics', 'search', query, params] as const,
  stats: (id: string) => ['clinics', id, 'stats'] as const,
};

/* ✅ Queries */

export function useAllClinics(params: Ref<PaginationRequestDto> | PaginationRequestDto) {
  return useQuery({
    // Reactive Key: Refetches automatically when params change
    queryKey: computed(() => clinicKeys.list(unref(params))),
    queryFn: () => clinicsApi.getAll(unref(params)),
    staleTime: 1000 * 60 * 5, // 5 minutes
    placeholderData: keepPreviousData, // Prevents table flicker
  });
}

export function useActiveClinics(params: Ref<PaginationRequestDto> | PaginationRequestDto) {
  return useQuery({
    queryKey: computed(() => clinicKeys.active(unref(params))),
    queryFn: () => clinicsApi.getActive(unref(params)),
    staleTime: 1000 * 60 * 5,
    placeholderData: keepPreviousData,
  });
}

export function useSearchClinics(
  query: Ref<string> | string,
  params: Ref<PaginationRequestDto> | PaginationRequestDto,
) {
  return useQuery({
    queryKey: computed(() => clinicKeys.search(unref(query), unref(params))),
    queryFn: () => clinicsApi.search(unref(query), unref(params)),
    enabled: computed(() => !!unref(query)),
    placeholderData: keepPreviousData,
  });
}

export function useMyClinic() {
  return useQuery({
    queryKey: clinicKeys.me,
    queryFn: () => clinicsApi.getMyClinic(),
    staleTime: 1000 * 60 * 30, // 30 mins (rarely changes)
  });
}

export function useClinicById(id: Ref<string> | string) {
  return useQuery({
    queryKey: computed(() => clinicKeys.byId(unref(id))),
    queryFn: () => clinicsApi.getById(unref(id)),
    enabled: computed(() => !!unref(id)),
  });
}

export function useClinicStats(id: Ref<string> | string) {
  return useQuery({
    queryKey: computed(() => clinicKeys.stats(unref(id))),
    queryFn: () => clinicsApi.getStats(unref(id)),
    enabled: computed(() => !!unref(id)),
  });
}

/* ✅ Mutations */

export function useClinicActions() {
  const toast = useToastService();
  const queryClient = useQueryClient();

  const createMutation = useMutation({
    mutationFn: (dto: CreateClinicRequestDto) => clinicsApi.create(dto),
    onSuccess: () => {
      toast.success('Clinic created successfully');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: { id: string; dto: UpdateClinicRequestDto }) =>
      clinicsApi.update(data.id, data.dto),
    onSuccess: (data, variables) => {
      toast.success('Clinic updated successfully');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
      queryClient.invalidateQueries({ queryKey: clinicKeys.byId(variables.id) });
    },
  });

  const updateStatusMutation = useMutation({
    mutationFn: (data: { id: string; dto: UpdateClinicStatusDto }) =>
      clinicsApi.updateStatus(data.id, data.dto),
    onSuccess: (data, variables) => {
      toast.success('Clinic status updated');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
      queryClient.invalidateQueries({ queryKey: clinicKeys.byId(variables.id) });
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.activate(id),
    onSuccess: () => {
      toast.success('Clinic activated');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
    },
  });

  const suspendMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.suspend(id),
    onSuccess: () => {
      toast.success('Clinic suspended');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
    },
  });

  const softDeleteMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.softDelete(id),
    onSuccess: () => {
      toast.success('Clinic moved to trash');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
    },
  });

  const hardDeleteMutation = useMutation({
    mutationFn: (id: string) => clinicsApi.hardDelete(id),
    onSuccess: () => {
      toast.success('Clinic permanently deleted');
      queryClient.invalidateQueries({ queryKey: clinicKeys.all });
    },
  });

  return {
    // Actions (Direct Wrappers)
    createClinic: createMutation.mutate,
    updateClinic: (id: string, dto: UpdateClinicRequestDto) => updateMutation.mutate({ id, dto }),
    updateClinicStatus: (id: string, dto: UpdateClinicStatusDto) =>
      updateStatusMutation.mutate({ id, dto }),
    activateClinic: activateMutation.mutate,
    suspendClinic: suspendMutation.mutate,
    softDeleteClinic: softDeleteMutation.mutate,
    hardDeleteClinic: hardDeleteMutation.mutate,

    // Mutations (For Loading States)
    createMutation,
    updateMutation,
    updateStatusMutation,
    activateMutation,
    suspendMutation,
    softDeleteMutation,
    hardDeleteMutation,
  };
}
