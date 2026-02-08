<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import { useClinicBranches } from '@/composables/query/branches/useBranches';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import ProgressBar from 'primevue/progressbar';
  import { computed } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto; clinicId?: string }>();

  const { data: branchesData } = useClinicBranches(computed(() => props.clinicId || ''));

  const calc = (used: number, base: number, purchased: number, bonus: number) => {
    const limit = base + purchased + bonus;
    const remaining = Math.max(0, limit - used);
    const percent = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;
    return { used, limit, remaining, percent, purchased, bonus };
  };

  const branches = computed(() =>
    calc(
      branchesData.value?.length || 0,
      props.subscription.includedBranchesSnapshot || 0,
      props.subscription.purchasedBranches || 0,
      (props.subscription as any).bonusBranches || 0,
    ),
  );
</script>

<template>
  <BaseCard>
    <div>
      <div class="flex justify-between items-start mb-4">
        <div class="flex items-center gap-3">
          <div
            class="w-10 h-10 rounded-lg bg-orange-50 dark:bg-orange-900/20 text-orange-500 flex items-center justify-center"
          >
            <i class="pi pi-building text-lg"></i>
          </div>
          <span class="font-bold text-surface-900 dark:text-surface-0 text-lg">Branches</span>
        </div>
        <span
          class="text-xs font-medium text-surface-400 dark:text-surface-500 uppercase tracking-wide"
        >
          Capacity
        </span>
      </div>

      <div class="mt-2 mb-4">
        <div
          class="text-3xl font-bold text-surface-900 dark:text-surface-0 flex items-baseline gap-2"
        >
          {{ branches.used }}
          <span class="text-xl text-surface-400 dark:text-surface-500 font-medium">
            / {{ branches.limit }}
          </span>
        </div>
      </div>

      <ProgressBar
        :value="branches.percent"
        :show-value="false"
        class="!h-2.5 !bg-surface-100 dark:!bg-surface-800 !rounded-full mb-3"
        :pt="{ value: { class: '!bg-orange-500 !rounded-full' } }"
      />
    </div>

    <div
      class="flex justify-between items-center text-xs text-surface-500 dark:text-surface-400 font-medium"
    >
      <span class="text-surface-500">{{ branches.percent }}% Used</span>
      <span class="font-bold text-orange-600 dark:text-orange-400">
        {{ branches.remaining }} left
      </span>
    </div>
  </BaseCard>
</template>
