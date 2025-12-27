import { clinicsApi } from '@/api/modules/clinics';
import { rolesApi } from '@/api/modules/roles';
import { subscriptionsApi } from '@/api/modules/subscriptions';
import type { ClinicResponseDto, PlanDefinition, RoleResponseDto } from '@/types/backend';
import { useQuery } from '@tanstack/vue-query';

/**
 * 🆕 Fetch Subscription Plans
 */
export function usePlans() {
  return useQuery<PlanDefinition[]>({
    queryKey: ['dropdown', 'plans'],
    queryFn: async () => {
      return await subscriptionsApi.getPlans();
    },
    staleTime: 1000 * 60 * 60, // 1 hour
    retry: 2,
  });
}

/**
 * Fetch grouped roles
 */
export function useGroupedRoles() {
  return useQuery<RoleResponseDto[]>({
    queryKey: ['dropdown', 'roles', 'grouped'],
    queryFn: async () => {
      const { data } = await rolesApi.getAll({});
      return data;
    },
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}

/**
 * Fetch all roles
 */
export function useRoles() {
  return useQuery<RoleResponseDto[]>({
    queryKey: ['dropdown', 'roles', 'all'],
    queryFn: async () => {
      const response = await rolesApi.getAll({});
      return response.data;
    },
    staleTime: 1000 * 60 * 5,
    retry: 1,
  });
}

/**
 * Fetch clinics for dropdowns
 */
export function useClinics() {
  return useQuery<ClinicResponseDto[]>({
    queryKey: ['dropdown', 'clinics'],
    queryFn: async () => {
      // ✅ FIX: Cast to 'any' or ignore TS for optional enum filters if generated DTO is strict
      const result = await clinicsApi.getAllPaged({
        limit: 200,
        cursor: '',
        sortBy: 'Name',
        descending: false,
        filter: '',

        search: '',
        name: undefined,
        email: undefined,
        phone: undefined,

        // ⚠️ Force these to undefined/null even if TS complains, because Backend accepts null
        status: null as any,
        subscriptionType: null as any,

        subscriptionFee: undefined,
        createdAt: undefined,
        includeDeleted: false,
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
    queryKey: ['dropdown', 'clinics', query],
    enabled: !!query,
    queryFn: async () => {
      const result = await clinicsApi.getAllPaged({
        search: query,
        filter: query,

        limit: 50,
        cursor: '',
        sortBy: 'Name',
        descending: false,

        name: undefined,
        email: undefined,
        phone: undefined,

        // ⚠️ Force null/undefined
        status: null as any,
        subscriptionType: null as any,

        subscriptionFee: undefined,
        createdAt: undefined,
        includeDeleted: false,
      });

      return result.items ?? [];
    },
    staleTime: 1000 * 60 * 2,
  });
}

/**
 * Search roles dynamically
 */
export function useSearchRoles(query: string) {
  return useQuery<RoleResponseDto[]>({
    queryKey: ['dropdown', 'roles', 'search', query],
    enabled: !!query,
    queryFn: async () => {
      const response = await rolesApi.getAll({});
      const roles = response.data;
      const q = query.trim().toLowerCase();

      if (!q) return roles;

      return roles.filter((r) => {
        const name = r.name?.toLowerCase() ?? '';
        const desc = r.description?.toLowerCase() ?? '';
        return name.includes(q) || desc.includes(q);
      });
    },
    staleTime: 1000 * 60 * 2,
  });
}

/**
 * RoleSelect Smart Query
 */
export function useRoleSelectData(mode: 'admin' | 'clinic', clinicId?: string) {
  return useQuery<RoleResponseDto[]>({
    queryKey: ['dropdown', 'roles', 'select', mode, clinicId],
    queryFn: async () => {
      if (mode === 'admin') {
        const res = await rolesApi.getAll({});
        return res.data;
      }

      if (!clinicId) return [];

      const clinicRolesReq = rolesApi.getAll({ clinicId: clinicId });
      const res = await clinicRolesReq;
      return res.data;
    },
    enabled: mode === 'admin' || (!!clinicId && mode === 'clinic'),
    staleTime: 1000 * 60 * 5,
  });
}
