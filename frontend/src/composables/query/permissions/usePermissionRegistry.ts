// src/composables/query/permissions/usePermissionRegistry.ts
import { permissionsApi } from '@/api/modules/permissions';
import type { PermissionRegistryState } from '@/modules/permissions/permissionRegistry';
import {
  buildPermissionRegistry,
  emptyPermissionRegistry,
} from '@/modules/permissions/permissionRegistry';
import type { PermissionResponseDto } from '@/types/backend';
import { useQuery } from '@tanstack/vue-query';
import { computed } from 'vue';

export function usePermissionRegistry() {
  const query = useQuery<PermissionRegistryState>({
    queryKey: ['permissions', 'registry'],
    queryFn: async () => {
      const allPermissions: PermissionResponseDto[] = await permissionsApi.getAll();
      return buildPermissionRegistry(allPermissions);
    },
    staleTime: 5 * 60 * 1000, // 5 دقائق
    gcTime: 10 * 60 * 1000,
  });

  const registry = computed(() => query.data.value ?? emptyPermissionRegistry);

  return {
    ...query,
    registry,
  };
}
