// src/composables/query/branches/useBranches.ts
import { branchesApi } from '@/api/modules/branches';
import { keepPreviousData, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

export const branchKeys = {
  all: ['branches'] as const,
  list: (clinicId: string | null) => ['branches', 'list', clinicId] as const,
  byId: (id: string) => ['branches', id] as const,
};

export function useClinicBranches(clinicId: Ref<string> | string) {
  return useQuery({
    queryKey: computed(() => branchKeys.list(unref(clinicId))),
    queryFn: () => branchesApi.getAll({ clinicId: unref(clinicId) }),
    enabled: computed(() => !!unref(clinicId)),
    placeholderData: keepPreviousData,
  });
}
