<template>
  <BaseCard class="h-full flex flex-col gap-8">
    <!-- Header -->
    <div class="flex items-start gap-5 border-b border-surface-100 dark:border-surface-700 pb-8">
      <Avatar
        :image="stats.logoUrl"
        :label="!stats.logoUrl ? stats.name[0] : ''"
        size="xlarge"
        class="!w-24 !h-24 !text-4xl !rounded-2xl border-2 border-surface-100 dark:border-surface-700 shadow-sm bg-surface-50 text-surface-600 shrink-0"
      />
      <div class="flex-1 min-w-0 pt-1">
        <div class="flex items-center gap-3 mb-2">
          <h1
            class="text-2xl font-extrabold text-surface-900 dark:text-surface-0 tracking-tight leading-none truncate"
          >
            {{ stats.name }}
          </h1>
          <Tag
            :value="stats.isActive ? 'ACTIVE' : 'SUSPENDED'"
            :severity="stats.isActive ? 'success' : 'danger'"
            class="!text-[10px] !font-bold !px-2 !py-0.5"
          />
        </div>

        <div class="flex flex-col gap-1.5 text-sm text-surface-500 dark:text-surface-400">
          <div class="flex items-center gap-2">
            <span class="font-mono bg-surface-100 dark:bg-surface-800 px-1.5 rounded text-xs">
              ID: {{ stats.clinicId.substring(0, 8) }}
            </span>
          </div>
          <a
            :href="`https://${stats.identifier}.nabd.care`"
            target="_blank"
            class="hover:text-primary-500 transition-colors flex items-center gap-1.5 cursor-pointer w-fit"
          >
            <i class="pi pi-external-link text-xs"></i>
            {{ stats.identifier }}.nabd.care
          </a>
        </div>
      </div>
    </div>

    <!-- Contact Section -->
    <div>
      <h3
        class="text-xs font-bold text-surface-400 uppercase tracking-wider mb-4 flex items-center gap-2"
      >
        <i class="pi pi-address-book"></i>
        Contact Details
      </h3>

      <div v-if="isLoadingDetails" class="space-y-4">
        <Skeleton width="100%" height="2rem" />
        <Skeleton width="80%" height="2rem" />
      </div>

      <template v-else-if="fullClinic">
        <div class="space-y-4">
          <div
            class="flex items-center gap-4 p-3 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800/50 transition-colors"
          >
            <div
              class="w-8 h-8 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center text-surface-500"
            >
              <i class="pi pi-phone"></i>
            </div>
            <div class="flex-1">
              <div class="text-xs text-surface-500">Phone Number</div>
              <div class="text-sm font-medium text-surface-900 dark:text-surface-0">
                {{ fullClinic.phone || 'N/A' }}
              </div>
            </div>
          </div>

          <div
            class="flex items-center gap-4 p-3 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800/50 transition-colors"
          >
            <div
              class="w-8 h-8 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center text-surface-500"
            >
              <i class="pi pi-envelope"></i>
            </div>
            <div class="flex-1">
              <div class="text-xs text-surface-500">Email Address</div>
              <div class="text-sm font-medium text-surface-900 dark:text-surface-0 break-all">
                {{ fullClinic.email || 'N/A' }}
              </div>
            </div>
          </div>

          <div
            class="flex items-center gap-4 p-3 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800/50 transition-colors"
          >
            <div
              class="w-8 h-8 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center text-surface-500"
            >
              <i class="pi pi-globe"></i>
            </div>
            <div class="flex-1">
              <div class="text-xs text-surface-500">Website</div>
              <div
                class="text-sm font-medium text-surface-900 dark:text-surface-0 truncate max-w-[250px]"
              >
                {{ fullClinic.website || 'N/A' }}
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- System Configuration (New Section) -->
    <div>
      <h3
        class="text-xs font-bold text-surface-400 uppercase tracking-wider mb-4 flex items-center gap-2"
      >
        <i class="pi pi-cog"></i>
        System Configuration
      </h3>

      <div class="grid grid-cols-2 gap-4">
        <div
          class="flex flex-col gap-1 p-3 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-100 dark:border-surface-700"
        >
          <span class="text-[10px] uppercase font-bold text-surface-500">Currency & Locale</span>
          <div class="flex items-center gap-2">
            <Tag
              :value="stats.settings.currency"
              severity="secondary"
              class="!text-xs !px-2 !py-0.5 !font-mono"
            />
            <span class="text-sm font-medium text-surface-700 dark:text-surface-300">
              {{ stats.settings.locale }}
            </span>
          </div>
        </div>

        <div
          class="flex flex-col gap-1 p-3 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-100 dark:border-surface-700"
        >
          <span class="text-[10px] uppercase font-bold text-surface-500">Timezone</span>
          <div
            class="text-sm font-medium text-surface-900 dark:text-surface-0 truncate"
            :title="stats.settings.timeZone"
          >
            {{ stats.settings.timeZone.split('/')[1] || stats.settings.timeZone }}
          </div>
        </div>

        <div
          class="col-span-2 flex flex-col gap-1 p-3 rounded-lg bg-surface-50 dark:bg-surface-800 border border-surface-100 dark:border-surface-700"
        >
          <div class="flex justify-between items-center">
            <span class="text-[10px] uppercase font-bold text-surface-500">Patient Portal</span>
            <Tag
              :value="stats.settings.enablePatientPortal ? 'ENABLED' : 'DISABLED'"
              :severity="stats.settings.enablePatientPortal ? 'success' : 'secondary'"
              class="!text-[10px] !font-bold !px-2 !py-0.5"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Legal Section -->
    <div class="mt-auto pt-6 border-t border-surface-100 dark:border-surface-700">
      <h3
        class="text-xs font-bold text-surface-400 uppercase tracking-wider mb-4 flex items-center gap-2"
      >
        <i class="pi pi-building"></i>
        Legal & Location
      </h3>

      <div v-if="isLoadingDetails" class="space-y-4">
        <Skeleton width="100%" height="4rem" />
      </div>

      <template v-else-if="fullClinic">
        <div class="space-y-4">
          <div class="flex gap-3">
            <i class="pi pi-map-marker text-primary-500 mt-1"></i>
            <span class="text-sm text-surface-700 dark:text-surface-300 leading-relaxed">
              {{ fullClinic.address || 'Address not provided.' }}
            </span>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div
              class="bg-surface-50 dark:bg-surface-800 px-3 py-2 rounded border border-surface-100 dark:border-surface-700"
            >
              <div class="text-[10px] text-surface-500 uppercase">Tax ID</div>
              <div class="text-sm font-mono font-bold text-surface-900 dark:text-surface-0">
                {{ stats.taxNumber || 'N/A' }}
              </div>
            </div>
            <div
              class="bg-surface-50 dark:bg-surface-800 px-3 py-2 rounded border border-surface-100 dark:border-surface-700"
            >
              <div class="text-[10px] text-surface-500 uppercase">Reg No.</div>
              <div class="text-sm font-mono font-bold text-surface-900 dark:text-surface-0">
                {{ stats.registrationNumber || 'N/A' }}
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import { useClinicQueries } from '@/composables/query/clinics/useClinicQueries';
  import type { ClinicDashboardStatsDto } from '@/types/backend';
  import Avatar from 'primevue/avatar';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  const props = defineProps<{
    stats: ClinicDashboardStatsDto;
    clinicId: string;
  }>();

  // --- Data Fetching (Full Clinic Details) ---
  const { useClinicDetails } = useClinicQueries();
  const { data: fullClinic, isLoading: isLoadingDetails } = useClinicDetails(
    computed(() => props.clinicId),
  );
</script>
