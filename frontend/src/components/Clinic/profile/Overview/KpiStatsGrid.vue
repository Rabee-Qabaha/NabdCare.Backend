<template>
  <div class="grid grid-cols-2 gap-4">
    <!-- Staff -->
    <BaseCard>
      <div class="flex justify-between items-start mb-2">
        <span class="text-[10px] font-bold text-surface-500 dark:text-surface-400 uppercase">
          Active Staff
        </span>
        <i class="pi pi-users text-blue-500 bg-blue-50 dark:bg-blue-900/20 p-1.5 rounded-md"></i>
      </div>
      <div class="text-2xl font-extrabold text-surface-900 dark:text-surface-0">
        {{ stats.activeUsersCount }}
      </div>
      <div class="text-xs mt-1" :class="getGrowthColorClass(stats.staffGrowthRate)">
        <i class="pi text-[10px]" :class="getGrowthIcon(stats.staffGrowthRate)"></i>
        {{ formatGrowth(stats.staffGrowthRate) }}
      </div>
    </BaseCard>

    <!-- Patient -->
    <BaseCard>
      <div class="flex justify-between items-start mb-2">
        <span class="text-[10px] font-bold text-surface-500 dark:text-surface-400 uppercase">
          Patients
        </span>
        <i
          class="pi pi-user-plus text-teal-500 bg-teal-50 dark:bg-teal-900/20 p-1.5 rounded-md"
        ></i>
      </div>
      <div class="text-2xl font-extrabold text-surface-900 dark:text-surface-0">
        {{ stats.activePatientsCount }}
      </div>
      <div class="text-xs mt-1" :class="getGrowthColorClass(stats.patientGrowthRate)">
        <i class="pi text-[10px]" :class="getGrowthIcon(stats.patientGrowthRate)"></i>
        {{ formatGrowth(stats.patientGrowthRate) }}
      </div>
    </BaseCard>

    <!-- Branches -->
    <BaseCard>
      <div class="flex justify-between items-start mb-2">
        <span class="text-[10px] font-bold text-surface-500 dark:text-surface-400 uppercase">
          Branches
        </span>
        <i
          class="pi pi-building text-indigo-500 bg-indigo-50 dark:bg-indigo-900/20 p-1.5 rounded-md"
        ></i>
      </div>
      <div class="text-2xl font-extrabold text-surface-900 dark:text-surface-0">
        {{ stats.totalBranches }}
      </div>
      <div class="text-xs text-surface-500 mt-1">Total Centers</div>
    </BaseCard>

    <!-- System Age -->
    <BaseCard>
      <div class="flex justify-between items-start mb-2">
        <span class="text-[10px] font-bold text-surface-500 dark:text-surface-400 uppercase">
          System Age
        </span>
        <i
          class="pi pi-history text-orange-500 bg-orange-50 dark:bg-orange-900/20 p-1.5 rounded-md"
        ></i>
      </div>
      <div class="text-2xl font-extrabold text-surface-900 dark:text-surface-0">
        {{ calculateSystemAge(stats.createdAt) }}
      </div>
      <div class="text-xs text-surface-500 mt-1">Days Active</div>
    </BaseCard>
  </div>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import type { ClinicDashboardStatsDto } from '@/types/backend';

  interface ExtendedStats extends ClinicDashboardStatsDto {
    staffGrowthRate?: number;
    patientGrowthRate?: number;
  }

  defineProps<{
    stats: ExtendedStats;
  }>();

  // --- Logic Helpers ---

  const calculateSystemAge = (createdAt?: string | Date) => {
    if (!createdAt) return 0;
    const start = new Date(createdAt).getTime();
    const now = new Date().getTime();
    const diff = now - start;
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  };

  const formatGrowth = (value?: number) => {
    if (value === undefined || value === null) return '0%';
    return `${value > 0 ? '+' : ''}${value}%`;
  };

  const getGrowthColorClass = (value?: number) => {
    if (!value) return 'text-surface-500 bg-surface-100 dark:bg-surface-700';
    return value > 0
      ? 'text-emerald-700 bg-emerald-50 dark:bg-emerald-900/30 dark:text-emerald-400'
      : 'text-red-700 bg-red-50 dark:bg-red-900/30 dark:text-red-400';
  };

  const getGrowthIcon = (value?: number) => {
    if (!value) return 'pi-minus';
    return value > 0 ? 'pi-arrow-up' : 'pi-arrow-down';
  };
</script>
