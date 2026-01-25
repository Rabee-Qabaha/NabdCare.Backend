<template>
  <div v-if="stats" class="space-y-6">
    <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
      <BaseCard>
        <div class="flex justify-between items-start">
          <span
            class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase"
          >
            Active Staff
          </span>
          <div
            class="w-10 h-10 rounded-lg bg-blue-50 dark:bg-blue-900/20 text-blue-600 flex items-center justify-center"
          >
            <i class="pi pi-users text-lg"></i>
          </div>
        </div>
        <div class="flex items-baseline gap-2 mt-3">
          <span class="text-3xl font-extrabold text-surface-900 dark:text-surface-0">
            {{ stats.activeUsersCount }}
          </span>
          <span
            class="text-xs font-semibold px-2 py-0.5 rounded-full flex items-center gap-1"
            :class="getGrowthColorClass(stats.staffGrowthRate)"
          >
            <i class="pi text-[10px]" :class="getGrowthIcon(stats.staffGrowthRate)"></i>
            {{ formatGrowth(stats.staffGrowthRate) }}
          </span>
        </div>
      </BaseCard>
      <BaseCard>
        <div class="flex justify-between items-start">
          <span
            class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase"
          >
            Branch Network
          </span>
          <div
            class="w-10 h-10 rounded-lg bg-indigo-50 dark:bg-indigo-900/20 text-indigo-500 flex items-center justify-center"
          >
            <i class="pi pi-building text-lg"></i>
          </div>
        </div>
        <div class="flex items-baseline gap-2 mt-3">
          <span class="text-3xl font-extrabold text-surface-900 dark:text-surface-0">
            {{ stats.totalBranches }}
          </span>
          <span class="text-sm text-surface-500 font-medium">Centers</span>
        </div>
      </BaseCard>

      <BaseCard>
        <div class="flex justify-between items-start">
          <span
            class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase"
          >
            Patient Base
          </span>
          <div
            class="w-10 h-10 rounded-lg bg-teal-50 dark:bg-teal-900/20 text-teal-500 flex items-center justify-center"
          >
            <i class="pi pi-user-plus text-lg"></i>
          </div>
        </div>
        <div class="flex items-baseline gap-2 mt-3">
          <span class="text-3xl font-extrabold text-surface-900 dark:text-surface-0">
            {{ stats.activePatientsCount }}
          </span>
          <span
            class="text-xs font-semibold px-2 py-0.5 rounded-full flex items-center gap-1"
            :class="getGrowthColorClass(stats.patientGrowthRate)"
          >
            <i class="pi text-[10px]" :class="getGrowthIcon(stats.patientGrowthRate)"></i>
            {{ formatGrowth(stats.patientGrowthRate) }}
          </span>
        </div>
      </BaseCard>

      <BaseCard>
        <div class="flex justify-between items-start">
          <span
            class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase"
          >
            System Age
          </span>
          <div
            class="w-10 h-10 rounded-lg bg-orange-50 dark:bg-orange-900/20 text-orange-500 flex items-center justify-center"
          >
            <i class="pi pi-history text-lg"></i>
          </div>
        </div>
        <div class="flex items-baseline gap-2 mt-3">
          <span class="text-3xl font-extrabold text-surface-900 dark:text-surface-0">
            {{ calculateSystemAge(stats.createdAt) }}
          </span>
          <span class="text-sm text-surface-500 font-medium">Days</span>
        </div>
      </BaseCard>
    </div>
    <div class="grid grid-cols-1 lg:grid-cols-5 gap-6">
      <BaseCard class="lg:col-span-3">
        <div class="flex items-center gap-3 mb-6">
          <div class="p-2 bg-surface-50 dark:bg-surface-900 rounded-lg">
            <i class="pi pi-id-card text-surface-500 dark:text-surface-400 text-xl"></i>
          </div>
          <h3 class="font-bold text-lg text-surface-900 dark:text-surface-0">
            Clinic Identity & Access
          </h3>
        </div>

        <div class="flex flex-col md:flex-row gap-8">
          <div class="flex gap-5 items-center">
            <Avatar
              :image="stats.logoUrl"
              :label="!stats.logoUrl ? stats.name[0] : ''"
              size="xlarge"
              shape="square"
              class="!w-24 !h-24 !rounded-2xl text-4xl shadow-sm border border-surface-100 dark:border-surface-600 bg-surface-50 text-surface-600"
            />
            <div class="flex flex-col justify-center">
              <span
                class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase mb-1"
              >
                Legal Name
              </span>
              <h2
                class="text-2xl font-extrabold text-surface-900 dark:text-surface-0 leading-tight"
              >
                {{ stats.name }}
              </h2>
              <a
                :href="`https://${stats.identifier}.nabd.care`"
                target="_blank"
                class="text-primary-500 hover:text-primary-600 hover:underline text-sm font-medium mt-1 flex items-center gap-1"
              >
                {{ stats.identifier }}.nabd.care
                <i class="pi pi-external-link text-xs"></i>
              </a>
            </div>
          </div>

          <div
            class="border-l border-surface-100 dark:border-surface-700 pl-0 md:pl-8 space-y-4 flex-1 py-2"
          >
            <div>
              <div
                class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase mb-1"
              >
                Primary Admin
              </div>
              <div
                class="flex items-center gap-2 text-surface-900 dark:text-surface-0 font-semibold"
              >
                <div class="p-1.5 bg-surface-100 dark:bg-surface-700 rounded-md">
                  <i class="pi pi-envelope text-surface-500 dark:text-surface-300 text-sm"></i>
                </div>
                {{ stats.primaryAdminName || 'admin@nabd.care' }}
              </div>
            </div>
            <div>
              <div
                class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase mb-1"
              >
                Security
              </div>
              <div
                class="flex items-center gap-2 text-surface-600 dark:text-surface-300 text-sm font-medium"
              >
                <div class="p-1.5 bg-surface-100 dark:bg-surface-700 rounded-md">
                  <i class="pi pi-clock text-surface-500 dark:text-surface-300 text-sm"></i>
                </div>
                <span>Last Login: {{ formatDate(stats.lastLoginAt) }}</span>
              </div>
            </div>
          </div>
        </div>
      </BaseCard>

      <BaseCard class="lg:col-span-2">
        <div class="flex items-center gap-3 mb-6">
          <div class="p-2 bg-surface-50 dark:bg-surface-900 rounded-lg">
            <i class="pi pi-file-check text-surface-500 dark:text-surface-400 text-xl"></i>
          </div>
          <h3 class="font-bold text-lg text-surface-900 dark:text-surface-0">Compliance Info</h3>
        </div>

        <div class="space-y-4">
          <div
            class="bg-surface-50 dark:bg-surface-800 p-4 rounded-lg border border-transparent dark:border-surface-700"
          >
            <div
              class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase mb-2"
            >
              Registration Number
            </div>
            <div class="font-mono text-xl text-surface-900 dark:text-surface-0 font-bold">
              {{ stats.registrationNumber || 'NOT-SET' }}
            </div>
          </div>

          <div
            class="bg-surface-50 dark:bg-surface-800 p-4 rounded-lg border border-transparent dark:border-surface-700"
          >
            <div
              class="text-xs font-bold text-surface-500 dark:text-surface-400 tracking-wider uppercase mb-2"
            >
              Tax ID / VAT
            </div>
            <div class="font-mono text-xl text-surface-900 dark:text-surface-0 font-bold">
              {{ stats.taxNumber || 'NOT-SET' }}
            </div>
          </div>
        </div>
      </BaseCard>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <BaseCard>
        <div class="flex justify-between items-center mb-8">
          <div class="flex items-center gap-3">
            <div class="p-2 bg-surface-50 dark:bg-surface-900 rounded-lg">
              <i class="pi pi-chart-bar text-surface-500 dark:text-surface-400 text-xl"></i>
            </div>
            <h3 class="font-bold text-lg text-surface-900 dark:text-surface-0">
              Resource Utilization
            </h3>
          </div>
          <Tag
            value="Current Plan"
            severity="secondary"
            class="!bg-surface-100 dark:!bg-surface-700 !text-surface-600 dark:!text-surface-300"
          ></Tag>
        </div>

        <div class="space-y-8">
          <div>
            <div class="flex justify-between mb-3 text-sm font-bold">
              <span class="text-surface-700 dark:text-surface-200 flex items-center gap-2">
                <i class="pi pi-building text-surface-400"></i>
                Branches
              </span>
              <span class="text-surface-900 dark:text-surface-0">
                {{ stats.totalBranches }}
                <span class="text-surface-400">/ 5</span>
              </span>
            </div>
            <ProgressBar
              :value="(stats.totalBranches / 5) * 100"
              :showValue="false"
              class="!h-3 !bg-surface-100 dark:!bg-surface-800 !rounded-full"
              :pt="{ value: { class: '!bg-primary-500 !rounded-full' } }"
            />
          </div>

          <div>
            <div class="flex justify-between mb-3 text-sm font-bold">
              <span class="text-surface-700 dark:text-surface-200 flex items-center gap-2">
                <i class="pi pi-users text-surface-400"></i>
                User Seats
              </span>
              <span class="text-surface-900 dark:text-surface-0">
                {{ stats.activeUsersCount }}
                <span class="text-surface-400">/ 20</span>
              </span>
            </div>
            <ProgressBar
              :value="(stats.activeUsersCount / 20) * 100"
              :showValue="false"
              class="!h-3 !bg-surface-100 dark:!bg-surface-800 !rounded-full"
              :pt="{ value: { class: '!bg-orange-500 !rounded-full' } }"
            />
          </div>
        </div>
      </BaseCard>

      <BaseCard>
        <div class="flex items-center gap-3 mb-6">
          <div class="p-2 bg-surface-50 dark:bg-surface-900 rounded-lg">
            <i class="pi pi-cog text-surface-500 dark:text-surface-400 text-xl"></i>
          </div>
          <h3 class="font-bold text-lg text-surface-900 dark:text-surface-0">Configuration</h3>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-6">
          <!-- Currency -->
          <div
            class="py-4 px-4 rounded-xl border border-transparent dark:border-surface-700 flex items-center gap-3 bg-surface-50 dark:bg-surface-800"
          >
            <div
              class="w-10 h-10 rounded-lg bg-white dark:bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 flex items-center justify-center text-lg shrink-0 shadow-sm dark:shadow-none"
            >
              <i class="pi pi-money-bill"></i>
            </div>
            <div>
              <div
                class="text-xs font-bold text-surface-500 dark:text-surface-400 uppercase tracking-wider mb-1"
              >
                Currency
              </div>
              <div class="font-extrabold text-surface-900 dark:text-surface-0 leading-none">
                {{ stats.settings.currency }}
              </div>
            </div>
          </div>

          <div
            class="py-4 px-4 rounded-xl border border-transparent dark:border-surface-700 flex items-center gap-3 bg-surface-50 dark:bg-surface-800"
          >
            <div
              class="w-10 h-10 rounded-lg bg-white dark:bg-blue-500/20 text-blue-600 dark:text-blue-400 flex items-center justify-center text-lg shrink-0 shadow-sm dark:shadow-none"
            >
              <i class="pi pi-globe"></i>
            </div>
            <div class="overflow-hidden">
              <div
                class="text-xs font-bold text-surface-500 dark:text-surface-400 uppercase tracking-wider mb-1"
              >
                Timezone
              </div>
              <div
                class="font-extrabold text-surface-900 dark:text-surface-0 truncate leading-none text-sm"
                :title="stats.settings.timeZone"
              >
                {{ stats.settings.timeZone }}
              </div>
            </div>
          </div>

          <div
            class="py-4 px-4 rounded-xl border border-transparent dark:border-surface-700 flex items-center gap-3 bg-surface-50 dark:bg-surface-800"
          >
            <div
              class="w-10 h-10 rounded-lg bg-white dark:bg-indigo-500/20 text-indigo-600 dark:text-indigo-400 flex items-center justify-center text-lg shrink-0 shadow-sm dark:shadow-none"
            >
              <i class="pi pi-language"></i>
            </div>
            <div>
              <div
                class="text-xs font-bold text-surface-500 dark:text-surface-400 uppercase tracking-wider mb-1"
              >
                Locale
              </div>
              <div class="font-extrabold text-surface-900 dark:text-surface-0 leading-none">
                {{ stats.settings.locale }}
              </div>
            </div>
          </div>

          <div
            class="py-4 px-4 rounded-xl border border-transparent dark:border-surface-700 flex items-center gap-3 bg-surface-50 dark:bg-surface-800"
          >
            <div
              class="w-10 h-10 rounded-lg bg-white dark:bg-purple-500/20 text-purple-600 dark:text-purple-400 flex items-center justify-center text-lg shrink-0 shadow-sm dark:shadow-none"
            >
              <i class="pi pi-calendar"></i>
            </div>
            <div>
              <div
                class="text-xs font-bold text-surface-500 dark:text-surface-400 uppercase tracking-wider mb-1"
              >
                Date Fmt
              </div>
              <div class="font-extrabold text-sm text-surface-900 dark:text-surface-0 leading-none">
                {{ stats.settings.dateFormat }}
              </div>
            </div>
          </div>
        </div>

        <div
          class="mt-auto w-full p-4 rounded-xl flex items-center justify-between border transition-colors bg-surface-50 dark:bg-surface-800 border-surface-100 dark:border-surface-700"
        >
          <div class="flex items-center gap-4">
            <div
              class="w-10 h-10 rounded-full flex items-center justify-center text-white shrink-0 shadow-sm bg-surface-400 dark:bg-surface-600"
            >
              <i class="pi pi-lock text-base font-bold"></i>
            </div>
            <div class="flex flex-col sm:flex-row sm:items-baseline sm:gap-2">
              <span class="font-bold text-base text-surface-700 dark:text-surface-300">
                Patient Portal
              </span>
              <span class="text-sm font-medium text-surface-500 dark:text-surface-400">
                <span class="hidden sm:inline text-surface-300 dark:text-surface-600 mr-2">•</span>
                Coming Soon
              </span>
            </div>
          </div>
          <i class="pi pi-shield text-surface-300 dark:text-surface-600 text-xl"></i>
        </div>
      </BaseCard>
    </div>
  </div>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import type { ClinicDashboardStatsDto } from '@/types/backend';
  import Avatar from 'primevue/avatar';
  import ProgressBar from 'primevue/progressbar';
  import Tag from 'primevue/tag';

  // Define properties with potential growth rates added
  interface ExtendedStats extends ClinicDashboardStatsDto {
    staffGrowthRate?: number;
    patientGrowthRate?: number;
  }

  const props = defineProps<{
    stats: ExtendedStats;
    clinicId: string;
  }>();

  // --- Logic Helpers ---

  const formatDate = (date?: string | Date) => {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const calculateSystemAge = (createdAt?: string | Date) => {
    if (!createdAt) return 0;
    const start = new Date(createdAt).getTime();
    const now = new Date().getTime();
    const diff = now - start;
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  };

  // --- Growth Helpers ---

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
