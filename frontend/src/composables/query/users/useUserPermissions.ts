import { permissionsApi } from '@/api/modules/permissions';
import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
import { usePermissionRegistry } from '@/composables/query/permissions/usePermissionRegistry';
import type { PermissionDefinition } from '@/modules/permissions/permissionRegistry';
import type { PermissionResponseDto } from '@/types/backend';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, ref, watch, type Ref } from 'vue';

export function useUserPermissions(userId: Ref<string | null>) {
  const { handleErrorAndNotify } = useErrorHandler();
  const { registry } = usePermissionRegistry();
  const queryClient = useQueryClient();

  // Local state for "Sticky" Optimistic UI
  const pendingUpdates = ref(new Map<string, { checked: boolean; ts: number }>());

  const effectiveKey = computed(() => ['user-permissions-effective', userId.value]);
  const overridesKey = computed(() => ['user-permissions-overrides', userId.value]);

  // -----------------------------------------------------------------------
  // QUERIES
  // -----------------------------------------------------------------------
  const effectiveQuery = useQuery({
    queryKey: effectiveKey,
    queryFn: async () => {
      if (!userId.value) return { permissions: [] };
      return await permissionsApi.getUserEffective(userId.value);
    },
    enabled: computed(() => !!userId.value),
    staleTime: 0,
    gcTime: 5 * 60 * 1000,
    placeholderData: keepPreviousData,
  });

  const overridesQuery = useQuery({
    queryKey: overridesKey,
    queryFn: async () => {
      if (!userId.value) return [];
      return await permissionsApi.getByUser(userId.value);
    },
    enabled: computed(() => !!userId.value),
    staleTime: 0,
    placeholderData: keepPreviousData,
  });

  const effectivePermissions = computed<PermissionResponseDto[]>(
    () => effectiveQuery.data.value?.permissions ?? [],
  );
  const overridePermissions = computed<PermissionResponseDto[]>(
    () => overridesQuery.data.value ?? [],
  );

  const effectiveIds = computed(() => new Set(effectivePermissions.value.map((p) => p.id)));
  const overrideIds = computed(() => new Set(overridePermissions.value.map((p) => p.id)));

  const isLoading = computed(
    () => effectiveQuery.isLoading.value || overridesQuery.isLoading.value,
  );

  // -----------------------------------------------------------------------
  // STATE SYNC
  // -----------------------------------------------------------------------
  watch(
    [overridePermissions, effectivePermissions],
    ([newOverrides, newEffective]) => {
      if (pendingUpdates.value.size === 0) return;

      const serverOverrideIds = new Set(newOverrides.map((p) => p.id));
      const serverEffectiveIds = new Set(newEffective.map((p) => p.id));

      const currentMap = new Map(pendingUpdates.value);
      let changed = false;

      currentMap.forEach((val, id) => {
        const isOverride = serverOverrideIds.has(id);
        const isEffective = serverEffectiveIds.has(id);

        if (val.checked === true) {
          if (isEffective && isOverride) {
            currentMap.delete(id);
            changed = true;
          }
        } else if (val.checked === false) {
          if (!isOverride) {
            currentMap.delete(id);
            changed = true;
          }
        }
      });

      if (changed) pendingUpdates.value = currentMap;
    },
    { deep: true },
  );

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
  // DATA COMPUTATION
  // -----------------------------------------------------------------------
  const categories = computed(() => {
    const effIds = effectiveIds.value;
    const ovIds = overrideIds.value;
    const pendingMap = pendingUpdates.value;
    const _trigger = pendingMap.size;
    void _trigger;

    return registry.value.categories.map((cat) => ({
      ...cat,
      items: cat.items.map((perm: PermissionDefinition) => {
        const id = perm.id;
        const isEffectiveServer = id ? effIds.has(id) : false;
        const isOverrideServer = id ? ovIds.has(id) : false;

        let isChecked = isEffectiveServer;
        if (id && pendingMap.has(id)) {
          isChecked = pendingMap.get(id)!.checked;
        }

        const isInherited = isEffectiveServer && !isOverrideServer;

        return {
          ...perm,
          checked: isChecked,
          inherited: isInherited,
          isCustom: !isInherited && isChecked,
        };
      }),
    }));
  });

  async function invalidateAll() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: effectiveKey.value }),
      queryClient.invalidateQueries({ queryKey: overridesKey.value }),
    ]);
  }

  // -----------------------------------------------------------------------
  // MUTATIONS
  // -----------------------------------------------------------------------
  const assignMutation = useMutation({
    mutationFn: (permissionId: string) =>
      permissionsApi.assignToUser({ userId: userId.value!, permissionId }),
    onError: (err: any) => {
      if (err?.httpStatus !== 409) handleErrorAndNotify(err);
    },
  });

  const removeMutation = useMutation({
    mutationFn: (permissionId: string) =>
      permissionsApi.removeFromUser(userId.value!, permissionId),
    onError: (err) => handleErrorAndNotify(err),
  });

  // ✅ Reset Mutation
  const resetMutation = useMutation({
    mutationFn: () => permissionsApi.resetUserPermissions(userId.value!),
    onSuccess: () => {
      pendingUpdates.value.clear();
      invalidateAll();
    },
    onError: (err) => handleErrorAndNotify(err),
  });

  // ✅ Combined Loading State
  const isMutating = computed(
    () =>
      assignMutation.isPending.value ||
      removeMutation.isPending.value ||
      resetMutation.isPending.value,
  );

  // ✅ Helper for UI
  const hasCustomPermissions = computed(() => overridePermissions.value.length > 0);

  // -----------------------------------------------------------------------
  // ACTIONS
  // -----------------------------------------------------------------------
  function setOptimistic(ids: string[], state: boolean) {
    const newMap = new Map(pendingUpdates.value);
    const now = Date.now();
    ids.forEach((id) => newMap.set(id, { checked: state, ts: now }));
    pendingUpdates.value = newMap;
  }

  async function togglePermission(permissionId: string, nextChecked: boolean) {
    if (!userId.value) return;
    setOptimistic([permissionId], nextChecked);

    const effIds = effectiveIds.value;
    const ovIds = overrideIds.value;
    const isCurrentlyEffective = effIds.has(permissionId);
    const isCurrentlyOverride = ovIds.has(permissionId);
    const isInherited = isCurrentlyEffective && !isCurrentlyOverride;

    try {
      if (nextChecked) {
        if (isCurrentlyEffective) return;
        await assignMutation.mutateAsync(permissionId);
      } else {
        if (isInherited) {
          const cleanup = new Map(pendingUpdates.value);
          cleanup.delete(permissionId);
          pendingUpdates.value = cleanup;
          return;
        }
        if (!isCurrentlyOverride) return;
        await removeMutation.mutateAsync(permissionId);
      }
      await invalidateAll();
    } catch (e) {
      const cleanup = new Map(pendingUpdates.value);
      cleanup.delete(permissionId);
      pendingUpdates.value = cleanup;
    }
  }

  async function toggleCategory(category: any, targetState: boolean) {
    if (!userId.value) return;
    if (isMutating.value) return;

    const effIds = effectiveIds.value;
    const ovIds = overrideIds.value;
    const idsInBatch: string[] = [];
    const ops: Promise<unknown>[] = [];
    const currentPending = pendingUpdates.value;

    category.items.forEach((perm: any) => {
      const id = perm.id;
      if (!id) return;

      const isPending = currentPending.has(id);
      const pendingVal = isPending ? currentPending.get(id)!.checked : null;
      const serverEffective = effIds.has(id);
      const serverOverride = ovIds.has(id);
      const isInherited = serverEffective && !serverOverride;

      if (isInherited) return;

      const currentlyChecked = isPending ? pendingVal : serverEffective;
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
      await invalidateAll();
    } catch (e) {
      console.error(e);
    }
  }

  // ✅ Reset Action
  async function resetToRole() {
    if (!userId.value) return;
    await resetMutation.mutateAsync();
  }

  return {
    permissionsQuery: effectiveQuery,
    categories,
    togglePermission,
    toggleCategory,
    isMutating,
    isLoading,
    resetToRole,
    hasCustomPermissions,
  };
}
