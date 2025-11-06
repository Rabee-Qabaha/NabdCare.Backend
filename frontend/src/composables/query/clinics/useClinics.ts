import { clinicsApi } from '@/api/modules/clinics';
import { useMutationWithInvalidate } from '@/composables/query/helpers/useMutationWithInvalidate';
import { useQueryWithToasts } from '@/composables/query/helpers/useQueryWithToasts';
import type {
  ClinicResponseDto,
  CreateClinicRequestDto,
  PaginatedResult,
  PaginationRequestDto,
  UpdateClinicRequestDto,
  UpdateClinicStatusDto,
} from '@/types/backend';

/* 🔹 Cache keys */
export const clinicKeys = {
  all: ['clinics'] as const,
  active: ['clinics', 'active'] as const,
  byId: (id: string) => ['clinics', id] as const,
  me: ['clinics', 'me'] as const,
  search: (query: string, params?: PaginationRequestDto) =>
    ['clinics', 'search', query, params] as const,
  stats: (id: string) => ['clinics', id, 'stats'] as const,
};

/* ✅ Queries */

export function useAllClinics(params: PaginationRequestDto) {
  return useQueryWithToasts<PaginatedResult<ClinicResponseDto>>({
    queryKey: clinicKeys.all,
    queryFn: () => clinicsApi.getAll(params),
    successMessage: 'Clinics loaded successfully.',
    errorMessage: 'Failed to load clinics.',
  });
}

export function useActiveClinics(params: PaginationRequestDto) {
  return useQueryWithToasts<PaginatedResult<ClinicResponseDto>>({
    queryKey: clinicKeys.active,
    queryFn: () => clinicsApi.getActive(params),
    successMessage: 'Active clinics loaded successfully.',
    errorMessage: 'Failed to load active clinics.',
  });
}

export function useSearchClinics(query: string, params: PaginationRequestDto) {
  return useQueryWithToasts<PaginatedResult<ClinicResponseDto>>({
    queryKey: clinicKeys.search(query, params),
    queryFn: () => clinicsApi.search(query, params),
    enabled: !!query,
    successMessage: 'Search completed successfully.',
    errorMessage: 'Search failed.',
  });
}

export function useMyClinic() {
  return useQueryWithToasts<ClinicResponseDto>({
    queryKey: clinicKeys.me,
    queryFn: () => clinicsApi.getMyClinic(),
    successMessage: 'Fetched current clinic info.',
    errorMessage: 'Failed to fetch clinic info.',
  });
}

export function useClinicById(id: string) {
  return useQueryWithToasts<ClinicResponseDto>({
    queryKey: clinicKeys.byId(id),
    queryFn: () => clinicsApi.getById(id),
    enabled: !!id,
    successMessage: 'Clinic loaded successfully.',
    errorMessage: 'Failed to load clinic.',
  });
}

export function useClinicStats(id: string) {
  return useQueryWithToasts({
    queryKey: clinicKeys.stats(id),
    queryFn: () => clinicsApi.getStats(id),
    enabled: !!id,
    successMessage: 'Clinic statistics loaded successfully.',
    errorMessage: 'Failed to load clinic statistics.',
  });
}

/* ✅ Mutations */

export function useCreateClinic() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'create'],
    mutationFn: (dto: CreateClinicRequestDto) => clinicsApi.create(dto),
    invalidateKeys: [clinicKeys.all],
    successMessage: 'Clinic created successfully!',
    errorMessage: 'Failed to create clinic.',
  });
}

export function useUpdateClinic() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'update'],
    mutationFn: (data: { id: string; dto: UpdateClinicRequestDto }) =>
      clinicsApi.update(data.id, data.dto),
    invalidateKeys: [(v) => clinicKeys.byId(v.id), clinicKeys.all],
    successMessage: 'Clinic updated successfully!',
    errorMessage: 'Failed to update clinic.',
  });
}

export function useUpdateClinicStatus() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'update-status'],
    mutationFn: (data: { id: string; dto: UpdateClinicStatusDto }) =>
      clinicsApi.updateStatus(data.id, data.dto),
    invalidateKeys: [(v) => clinicKeys.byId(v.id), clinicKeys.all],
    successMessage: 'Clinic status updated successfully!',
    errorMessage: 'Failed to update clinic status.',
  });
}

export function useActivateClinic() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'activate'],
    mutationFn: (id: string) => clinicsApi.activate(id),
    invalidateKeys: [clinicKeys.all],
    successMessage: 'Clinic activated successfully!',
    errorMessage: 'Failed to activate clinic.',
  });
}

export function useSuspendClinic() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'suspend'],
    mutationFn: (id: string) => clinicsApi.suspend(id),
    invalidateKeys: [clinicKeys.all],
    successMessage: 'Clinic suspended successfully!',
    errorMessage: 'Failed to suspend clinic.',
  });
}

export function useSoftDeleteClinic() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'soft-delete'],
    mutationFn: (id: string) => clinicsApi.softDelete(id),
    invalidateKeys: [clinicKeys.all],
    successMessage: 'Clinic deleted successfully!',
    errorMessage: 'Failed to delete clinic.',
  });
}

export function useHardDeleteClinic() {
  return useMutationWithInvalidate({
    mutationKey: ['clinics', 'hard-delete'],
    mutationFn: (id: string) => clinicsApi.hardDelete(id),
    invalidateKeys: [clinicKeys.all],
    successMessage: 'Clinic permanently deleted!',
    errorMessage: 'Failed to permanently delete clinic.',
  });
}
