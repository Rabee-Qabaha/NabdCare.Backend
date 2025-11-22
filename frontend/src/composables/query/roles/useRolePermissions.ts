import { rolesApi } from '@/api/modules/roles';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermissionRegistry } from '@/composables/query/permissions/usePermissionRegistry';
import type { PermissionDefinition } from '@/modules/permissions/permissionRegistry';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, ref, watch, type Ref } from 'vue';

export function useRolePermissions(roleId: Ref<string | null>) {
  const { handleErrorAndNotify } = useErrorHandler();
  const { registry } = usePermissionRegistry();
  const queryClient = useQueryClient();

  // 1. LOCAL STATE
  // Stores { checked: boolean, ts: timestamp } to handle "Sticky" UI
  const pendingUpdates = ref(new Map<string, { checked: boolean; ts: number }>());

  const queryKey = computed(() => ['role-permissions', roleId.value]);

  // -----------------------------------------------------------------------
  // 2. QUERY (Fixed Type)
  // -----------------------------------------------------------------------
  // [FIX] The API returns string[] (List of IDs), not PermissionResponseDto[]
  const permissionsQuery = useQuery<string[]>({
    queryKey,
    queryFn: async () => {
      if (!roleId.value) return [];
      const res = await rolesApi.getPermissions(roleId.value);
      return res.data; // This is ["guid1", "guid2", ...]
    },
    enabled: computed(() => !!roleId.value),
    staleTime: 0,
    gcTime: 5 * 60 * 1000,
    placeholderData: keepPreviousData,
  });

  // [FIX] Create a Set of IDs directly from the string array
  const serverPermissionIds = computed(() => new Set(permissionsQuery.data.value ?? []));

  const isLoading = computed(() => permissionsQuery.isLoading.value);

  // -----------------------------------------------------------------------
  // 3. REACTIVE CLEANUP (Sticky Fix)
  // -----------------------------------------------------------------------
  watch(
    serverPermissionIds,
    (serverIds) => {
      if (pendingUpdates.value.size === 0) return;

      const currentMap = new Map(pendingUpdates.value);
      let changed = false;

      currentMap.forEach((val, id) => {
        const serverHasIt = serverIds.has(id);

        // CASE 1: We wanted ON, Server says ON -> Cleanup
        if (val.checked === true && serverHasIt) {
          currentMap.delete(id);
          changed = true;
        }
        // CASE 2: We wanted OFF, Server says OFF -> Cleanup
        else if (val.checked === false && !serverHasIt) {
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

  // -----------------------------------------------------------------------
  // 4. DATA COMPUTATION (Fixed Matching)
  // -----------------------------------------------------------------------
  const categories = computed(() => {
    const serverIds = serverPermissionIds.value;
    const pendingMap = pendingUpdates.value;

    // Force reactivity
    const _trigger = pendingMap.size;
    void _trigger;

    return registry.value.categories.map((cat) => ({
      ...cat,
      items: cat.items.map((perm: PermissionDefinition) => {
        const id = perm.id;

        // [FIX] Match purely by ID
        const isAssignedServer = id ? serverIds.has(id) : false;

        // Optimistic Override
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

  // -----------------------------------------------------------------------
  // 5. MUTATIONS
  // -----------------------------------------------------------------------
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

  // -----------------------------------------------------------------------
  // 6. ACTIONS
  // -----------------------------------------------------------------------
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
