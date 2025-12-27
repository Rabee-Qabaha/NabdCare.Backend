// src/components/Subscription/ResourceUtilizationCard.vue
<script setup lang="ts">
  import { useClinicBranches } from '@/composables/query/branches/useBranches';
  import { useUsersPaged } from '@/composables/query/users/useUsers';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import ProgressBar from 'primevue/progressbar';
  import { computed } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto; clinicId?: string }>();

  const { data: branchesData } = useClinicBranches(computed(() => props.clinicId || ''));
  const { data: usersData } = useUsersPaged({ clinicId: props.clinicId, limit: 1 });

  const calc = (used: number, base: number, purchased: number, bonus: number) => {
    const limit = base + purchased + bonus;
    const remaining = Math.max(0, limit - used);
    const percent = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;
    return { used, limit, remaining, percent, purchased, bonus };
  };

  const users = computed(() =>
    calc(
      usersData.value?.totalCount || 0,
      props.subscription.includedUsersSnapshot || 0,
      props.subscription.purchasedUsers || 0,
      (props.subscription as any).bonusUsers || 0,
    ),
  );
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
  <div
    class="bg-surface-0 dark:bg-surface-800 rounded-xl border border-surface-200 dark:border-surface-700 shadow-sm flex flex-col h-full"
  >
    <div class="px-4 py-2 border-b border-surface-100 dark:border-surface-700">
      <h4
        class="text-[10px] font-bold text-surface-500 uppercase tracking-wider flex items-center gap-1.5"
      >
        <i class="pi pi-chart-pie"></i>
        Resource Utilization
      </h4>
    </div>

    <div class="p-4 flex-grow flex flex-col gap-3 justify-center">
      <div class="flex-1">
        <div class="flex justify-between items-end mb-1.5">
          <div class="flex items-center gap-2">
            <div
              class="w-8 h-8 rounded-full bg-purple-50 dark:bg-purple-900/20 text-purple-600 dark:text-purple-400 flex items-center justify-center"
            >
              <i class="pi pi-users text-sm"></i>
            </div>
            <div class="flex flex-col">
              <span class="text-xs font-bold text-surface-900 dark:text-surface-0 leading-none">
                Users
              </span>
              <span class="text-[10px] text-surface-500 leading-none mt-0.5">Accounts</span>
            </div>
          </div>
          <div class="text-right leading-none">
            <span class="text-sm font-bold text-surface-900 dark:text-surface-0">
              {{ users.used }}
            </span>
            <span class="text-xs text-surface-500 mx-0.5">/</span>
            <span class="text-xs text-surface-500">{{ users.limit }}</span>
          </div>
        </div>

        <ProgressBar
          :value="users.percent"
          :showValue="false"
          style="height: 6px"
          class="rounded-full bg-surface-100 dark:bg-surface-700 mb-1"
          :pt="{ value: { class: '!bg-purple-500 rounded-full' } }"
        />

        <div class="flex justify-between items-center text-[10px]">
          <span class="text-surface-500">{{ users.percent }}% Used</span>
          <span class="text-surface-400">{{ users.remaining }} left</span>
        </div>
      </div>

      <div class="h-px bg-surface-100 dark:bg-surface-700 w-full"></div>

      <div class="flex-1">
        <div class="flex justify-between items-end mb-1.5">
          <div class="flex items-center gap-2">
            <div
              class="w-8 h-8 rounded-full bg-orange-50 dark:bg-orange-900/20 text-orange-600 dark:text-orange-400 flex items-center justify-center"
            >
              <i class="pi pi-building text-sm"></i>
            </div>
            <div class="flex flex-col">
              <span class="text-xs font-bold text-surface-900 dark:text-surface-0 leading-none">
                Branches
              </span>
              <span class="text-[10px] text-surface-500 leading-none mt-0.5">Locations</span>
            </div>
          </div>
          <div class="text-right leading-none">
            <span class="text-sm font-bold text-surface-900 dark:text-surface-0">
              {{ branches.used }}
            </span>
            <span class="text-xs text-surface-500 mx-0.5">/</span>
            <span class="text-xs text-surface-500">{{ branches.limit }}</span>
          </div>
        </div>

        <ProgressBar
          :value="branches.percent"
          :showValue="false"
          style="height: 6px"
          class="rounded-full bg-surface-100 dark:bg-surface-700 mb-1"
          :pt="{
            value: {
              class: `!rounded-full ${branches.used > branches.limit ? '!bg-red-500' : '!bg-orange-500'}`,
            },
          }"
        />

        <div class="flex justify-between items-center text-[10px]">
          <template v-if="branches.used > branches.limit">
            <span class="text-red-500 font-bold flex items-center gap-1">
              <i class="pi pi-exclamation-triangle" style="font-size: 0.6rem"></i>
              Over Limit
            </span>
          </template>
          <template v-else>
            <span class="text-surface-500">{{ branches.percent }}% Used</span>
            <span class="text-surface-400">{{ branches.remaining }} left</span>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>
