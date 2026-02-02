<template>
  <BaseCard>
    <div class="flex items-center gap-2 mb-4">
      <i class="pi pi-chart-bar text-surface-400"></i>
      <h3 class="font-bold text-surface-900 dark:text-surface-0">Resource Utilization</h3>
    </div>
    <div class="space-y-5">
      <div>
        <div class="flex justify-between mb-1.5 text-xs font-bold">
          <span class="text-surface-500">Branches Used</span>
          <span class="text-surface-900 dark:text-surface-0">{{ totalBranches }} / 5</span>
        </div>
        <ProgressBar
          :value="(totalBranches / 5) * 100"
          :showValue="false"
          class="!h-2 !bg-surface-100 dark:!bg-surface-800"
          :pt="{ value: { class: '!bg-primary-500' } }"
        />
      </div>
      <div>
        <div class="flex justify-between mb-1.5 text-xs font-bold">
          <span class="text-surface-500">User Seats Used</span>
          <span class="text-surface-900 dark:text-surface-0">{{ activeUsersCount }} / 20</span>
        </div>
        <ProgressBar
          :value="(activeUsersCount / 20) * 100"
          :showValue="false"
          class="!h-2 !bg-surface-100 dark:!bg-surface-800"
          :pt="{ value: { class: '!bg-orange-500' } }"
        />
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import ProgressBar from 'primevue/progressbar';

  defineProps<{
    totalBranches: number;
    activeUsersCount: number;
  }>();
</script>
