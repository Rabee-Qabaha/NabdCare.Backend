// src/composables/useDropdownData.ts
import { useQuery } from "@tanstack/vue-query";
import { rolesApi } from "@/api/modules/roles";
import { clinicsApi } from "@/api/modules/clinics";
import type { ClinicResponseDto, RoleResponseDto } from "@/types/backend";

/**
 * Fetch grouped roles (system / clinic / template)
 * Uses rolesApi.getGrouped() which aggregates client-side.
 */
export function useGroupedRoles() {
  return useQuery({
    queryKey: ["dropdown", "roles", "grouped"],
    queryFn: () => rolesApi.getGrouped(),
    staleTime: 1000 * 60 * 5, // 5 minutes
    retry: 1,
  });
}

/**
 * Fetch all roles (flat list)
 */
export function useRoles() {
  return useQuery<RoleResponseDto[]>({
    queryKey: ["dropdown", "roles", "all"],
    queryFn: async () => {
      const response = await rolesApi.getAll();
      return response.data;
    },
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}

/**
 * Fetch clinics for dropdowns
 * NOTE: clinicsApi.getAll returns PaginatedResult<ClinicResponseDto>
 * We normalize to simple array here.
 */
export function useClinics() {
  return useQuery<ClinicResponseDto[]>({
    queryKey: ["dropdown", "clinics"],
    queryFn: async () => {
      const result = await clinicsApi.getAll({
        limit: 200,
        descending: false,
        cursor: null as any,
        sortBy: "",
        filter: "",
      });

      return result.items ?? [];
    },
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}

/**
 * Search clinics dynamically
 */
export function useSearchClinics(query: string) {
  return useQuery<ClinicResponseDto[]>({
    queryKey: ["dropdown", "clinics", query],
    enabled: !!query,
    queryFn: async () => {
      const result = await clinicsApi.search(query, {
        limit: 50,
        descending: false,
        cursor: null as any,
        sortBy: "",
        filter: "",
      });

      return result.items ?? [];
    },
    staleTime: 1000 * 60 * 2,
  });
}

/**
 * Search roles dynamically
 * No dedicated backend endpoint => filter client-side
 */
export function useSearchRoles(query: string) {
  return useQuery<RoleResponseDto[]>({
    queryKey: ["dropdown", "roles", "search", query],
    enabled: !!query,
    queryFn: async () => {
      const response = await rolesApi.getAll();
      const roles = response.data;
      const q = query.trim().toLowerCase();

      if (!q) return roles;

      return roles.filter((r) => {
        const name = r.name?.toLowerCase() ?? "";
        const desc = r.description?.toLowerCase() ?? "";
        return name.includes(q) || desc.includes(q);
      });
    },
    staleTime: 1000 * 60 * 2,
  });
}