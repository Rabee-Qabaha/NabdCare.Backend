import { DropdownDataService } from '@/service/api/DropdownDataService';
import type { ClinicResponseDto, RoleResponseDto } from '@/types/backend';
import { useQuery } from '@tanstack/vue-query';

/**
 * Composable: useDropdownData
 * ----------------------------------------------------------
 * Centralized access layer for dropdown data (roles, clinics)
 *
 * Features:
 * - Built on Vue Query for caching and refetch control
 * - Uses the new DropdownDataService (central API logic)
 * - Returns grouped role data ready for complex selects
 *
 * Updated: 2025-11-02
 */

/**
 * Fetch and group roles (system + clinic)
 *
 * Returns an object:
 * {
 *   systemRoles: RoleResponseDto[],
 *   clinicRoles: RoleResponseDto[],
 * }
 */
export function useGroupedRoles() {
  return useQuery({
    queryKey: ['dropdown', 'roles', 'grouped'],
    queryFn: async () => await DropdownDataService.fetchGroupedRoles(),
    staleTime: 1000 * 60 * 5, // cache 5 min
    retry: 1,
  });
}

/**
 * Fetch all roles (flat)
 */
export function useRoles() {
  return useQuery({
    queryKey: ['dropdown', 'roles', 'all'],
    queryFn: async () => await DropdownDataService.fetchRoles(),
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}

/**
 * Fetch all clinics
 */
export function useClinics() {
  return useQuery<ClinicResponseDto[]>({
    queryKey: ['dropdown', 'clinics'],
    queryFn: async () => await DropdownDataService.fetchClinics(),
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}

/**
 * Search clinics dynamically by query
 */
export function useSearchClinics(query: string) {
  return useQuery<ClinicResponseDto[]>({
    queryKey: ['dropdown', 'clinics', query],
    queryFn: async () => await DropdownDataService.searchClinics(query),
    enabled: !!query,
    staleTime: 1000 * 60 * 2,
  });
}

/**
 * Search roles dynamically by query
 */
export function useSearchRoles(query: string) {
  return useQuery<RoleResponseDto[]>({
    queryKey: ['dropdown', 'roles', query],
    queryFn: async () => await DropdownDataService.searchRoles(query),
    enabled: !!query,
    staleTime: 1000 * 60 * 2,
  });
}
