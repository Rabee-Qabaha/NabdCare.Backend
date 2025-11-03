import { useQuery } from "@tanstack/vue-query";
import { DropdownDataService } from "@/service/api/DropdownDataService";
import type { RoleResponseDto, ClinicResponseDto } from "@/types/backend";
import { computed } from "vue";

/**
 * Dropdown Data Composables
 * Location: src/composables/query/useDropdownData.ts
 *
 * Purpose:
 * - Provide Vue Query integration for dropdown data
 * - Handle caching (30 min for dropdowns - rarely change)
 * - Automatic retry with exponential backoff
 * - Error handling built-in
 *
 * Benefits:
 * - Reusable across entire app
 * - Automatic background refetching
 * - Stale-while-revalidate pattern
 * - Type-safe with TypeScript
 *
 * Updated: 2025-11-02
 */

// ========================================
// QUERIES
// ========================================

/**
 * Fetch roles for dropdowns with Vue Query caching
 *
 * Features:
 * - Caches for 30 minutes (roles rarely change)
 * - Supports system roles, templates, and clinic roles
 * - Shows user count and permission count
 * - Includes color codes and icons for UI
 *
 * Usage:
 * ```typescript
 * const { data: roles, isLoading, error } = useRoles();
 *
 * <Select :options="roles" :loading="isLoading" />
 * ```
 */
export function useRoles() {
  return useQuery<RoleResponseDto[], Error>({
    queryKey: ["dropdowns", "roles"],
    queryFn: () => DropdownDataService.fetchRoles(),
    staleTime: 30 * 60 * 1000, // 30 minutes
    gcTime: 60 * 60 * 1000, // 1 hour garbage collection
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 10000),
    refetchOnWindowFocus: false,
    refetchOnMount: true,
  });
}

/**
 * Fetch clinics for dropdowns with Vue Query caching
 *
 * Cache strategy: Same as roles (30 min stale)
 *
 * Usage:
 * ```typescript
 * const { data: clinics, isLoading } = useClinics();
 * ```
 */
export function useClinics() {
  return useQuery<ClinicResponseDto[], Error>({
    queryKey: ["dropdowns", "clinics"],
    queryFn: () => DropdownDataService.fetchClinics(),
    staleTime: 30 * 60 * 1000,
    gcTime: 60 * 60 * 1000,
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 10000),
    refetchOnWindowFocus: false,
    refetchOnMount: true,
  });
}

/**
 * Search clinics with query term
 *
 * Cache strategy: Different from useClinics (5 min stale)
 * - Search results are more volatile
 * - Only fetch if search term is provided
 *
 * Usage:
 * ```typescript
 * const searchTerm = ref("");
 * const { data: searchResults, isLoading } = useSearchClinics(searchTerm);
 * ```
 */
export function useSearchClinics(searchTerm: string) {
  return useQuery<ClinicResponseDto[], Error>({
    queryKey: ["dropdowns", "clinics", "search", searchTerm],
    queryFn: () => DropdownDataService.searchClinics(searchTerm),
    enabled: searchTerm.length > 0,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: 1,
    retryDelay: 1000,
  });
}

/**
 * Search roles with query term
 *
 * Usage:
 * ```typescript
 * const { data: searchResults } = useSearchRoles(searchTerm);
 * ```
 */
export function useSearchRoles(searchTerm: string) {
  return useQuery<RoleResponseDto[], Error>({
    queryKey: ["dropdowns", "roles", "search", searchTerm],
    queryFn: () => DropdownDataService.searchRoles(searchTerm),
    enabled: searchTerm.length > 0,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: 1,
    retryDelay: 1000,
  });
}

/**
 * Get only system roles (SuperAdmin, Admin, etc.)
 *
 * Useful for filtering role selects to show only system roles
 */
export function useSystemRoles() {
  const { data: allRoles, ...rest } = useRoles();

  const systemRoles = computed(() => {
    return (allRoles.value || []).filter((r) => r.isSystemRole);
  });

  return {
    data: systemRoles,
    ...rest,
  };
}

/**
 * Get only template roles (for cloning)
 *
 * Useful for showing templates that clinics can clone
 */
export function useTemplateRoles() {
  const { data: allRoles, ...rest } = useRoles();

  const templateRoles = computed(() => {
    return (allRoles.value || []).filter((r) => r.isTemplate);
  });

  return {
    data: templateRoles,
    ...rest,
  };
}

/**
 * Get roles grouped by type
 *
 * Returns:
 * - systemRoles: System-level roles
 * - templateRoles: Template roles for cloning
 * - clinicRoles: Clinic-specific roles
 */
export function useGroupedRoles() {
  const { data: allRoles, ...rest } = useRoles();

  const groupedRoles = computed(() => {
    const roles = allRoles.value || [];
    return {
      systemRoles: roles.filter((r) => r.isSystemRole && !r.isTemplate),
      templateRoles: roles.filter((r) => r.isTemplate),
      clinicRoles: roles.filter(
        (r) => !r.isSystemRole && !r.isTemplate && r.clinicId
      ),
    };
  });

  return {
    data: groupedRoles,
    ...rest,
  };
}
