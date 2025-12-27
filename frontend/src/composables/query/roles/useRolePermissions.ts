// src/composables/query/roles/useRolePermissions.ts
import { rolesApi } from '@/api/modules/roles';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermissionRegistry } from '@/composables/query/permissions/usePermissionRegistry';
import type { PermissionDefinition } from '@/modules/permissions/permissionRegistry';
import type { PermissionResponseDto } from '@/types/backend';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, ref, watch, type Ref } from 'vue';

export function useRolePermissions(roleId: Ref<string | null>) {
  const { handleErrorAndNotify } = useErrorHandler();
  const { registry } = usePermissionRegistry();
  const queryClient = useQueryClient();

  const pendingUpdates = ref(new Map<string, { checked: boolean; ts: number }>());
  const queryKey = computed(() => ['role-permissions', roleId.value]);

  // 1. QUERY (Corrected Types)
  const permissionsQuery = useQuery<PermissionResponseDto[]>({
    queryKey,
    queryFn: async () => {
      if (!roleId.value) return [];
      const res = await rolesApi.getPermissions(roleId.value);

      // SAFETY CHECK: If backend actually sends strings ["id1", "id2"] but DTO says objects
      // We can force cast or map here if needed.
      // Assuming DTO is correct (returns Objects):
      return res.data || [];
    },
    enabled: computed(() => !!roleId.value),
    staleTime: 0,
    gcTime: 5 * 60 * 1000,
    placeholderData: keepPreviousData,
  });

  // 2. Computed Set of IDs
  const serverPermissionIds = computed(() => {
    const data = permissionsQuery.data.value ?? [];

    // Handle if data is array of strings (backend discrepancy)
    if (data.length > 0 && typeof data[0] === 'string') {
      return new Set(data as unknown as string[]);
    }

    // Standard case: Array of Objects
    return new Set(data.map((p) => p.id));
  });

  const isLoading = computed(() => permissionsQuery.isLoading.value);

  // 3. REACTIVE CLEANUP
  watch(
    serverPermissionIds,
    (serverIds) => {
      if (pendingUpdates.value.size === 0) return;

      const currentMap = new Map(pendingUpdates.value);
      let changed = false;

      currentMap.forEach((val, id) => {
        const serverHasIt = serverIds.has(id);

        if (val.checked === true && serverHasIt) {
          currentMap.delete(id);
          changed = true;
        } else if (val.checked === false && !serverHasIt) {
          currentMap.delete(id);
          changed = true;
        }
      });

      if (changed) pendingUpdates.value = currentMap;
    },
    { deep: true },
  );

  // Safety cleanup
  setInterval(() => {
    if (pendingUpdates.value.size === 0) return;
    const now = Date.now();
    const currentMap = new Map(pendingUpdates.value);
    let expired = false;
    currentMap.forEach((val, key) => {
      if (now - val.ts > 5000) {
        currentMap.delete(key);
        expired = true;
      }
    });
    if (expired) pendingUpdates.value = currentMap;
  }, 2000);

  // 4. DATA COMPUTATION
  const categories = computed(() => {
    const serverIds = serverPermissionIds.value;
    const pendingMap = pendingUpdates.value;
    const _trigger = pendingMap.size;
    void _trigger;

    return registry.value.categories.map((cat) => ({
      ...cat,
      items: cat.items.map((perm: PermissionDefinition) => {
        const id = perm.id;

        // Check Server State
        // Note: serverIds is Set<string>, id is string. Perfect match.
        const isAssignedServer = id ? serverIds.has(id) : false;

        // Check Pending State
        let isChecked = isAssignedServer;

        if (id && pendingMap.has(id)) {
          isChecked = pendingMap.get(id)!.checked;
        }

        return {
          ...perm,
          checked: isChecked,
        };
      }),
    }));
  });

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: queryKey.value });
  }

  // 5. MUTATIONS
  const assignMutation = useMutation({
    mutationFn: (permissionId: string) => rolesApi.assignPermission(roleId.value!, permissionId),
    onError: (err: any) => handleErrorAndNotify(err),
  });

  const removeMutation = useMutation({
    mutationFn: (permissionId: string) => rolesApi.removePermission(roleId.value!, permissionId),
    onError: (err) => handleErrorAndNotify(err),
  });

  const isMutating = computed(
    () => assignMutation.isPending.value || removeMutation.isPending.value,
  );

  // 6. ACTIONS
  function setOptimistic(ids: string[], state: boolean) {
    const newMap = new Map(pendingUpdates.value);
    const now = Date.now();
    ids.forEach((id) => newMap.set(id, { checked: state, ts: now }));
    pendingUpdates.value = newMap;
  }

  async function togglePermission(permissionId: string, nextChecked: boolean) {
    if (!roleId.value) return;
    setOptimistic([permissionId], nextChecked);

    try {
      if (nextChecked) {
        await assignMutation.mutateAsync(permissionId);
      } else {
        await removeMutation.mutateAsync(permissionId);
      }
      await invalidate();
    } catch (e) {
      const cleanup = new Map(pendingUpdates.value);
      cleanup.delete(permissionId);
      pendingUpdates.value = cleanup;
    }
  }

  async function toggleCategory(category: any, targetState: boolean) {
    if (!roleId.value) return;
    if (isMutating.value) return;

    const idsInBatch: string[] = [];
    const ops: Promise<unknown>[] = [];
    const currentPending = pendingUpdates.value;
    const serverIds = serverPermissionIds.value;

    category.items.forEach((perm: any) => {
      const id = perm.id;
      if (!id) return;

      const isPending = currentPending.has(id);
      const pendingVal = isPending ? currentPending.get(id)!.checked : null;
      const serverVal = serverIds.has(id);

      const currentlyChecked = isPending ? pendingVal : serverVal;

      if (currentlyChecked === targetState) return;

      idsInBatch.push(id);

      if (targetState) {
        ops.push(assignMutation.mutateAsync(id));
      } else {
        ops.push(removeMutation.mutateAsync(id));
      }
    });

    if (!ops.length) return;

    setOptimistic(idsInBatch, targetState);

    try {
      await Promise.allSettled(ops);
      await invalidate();
    } catch (e) {
      console.error(e);
    }
  }

  return {
    permissionsQuery,
    categories,
    togglePermission,
    toggleCategory,
    isMutating,
    isLoading,
  };
}
