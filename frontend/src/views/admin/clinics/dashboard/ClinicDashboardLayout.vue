<template>
  <div
    class="flex flex-col min-h-screen bg-surface-50 dark:bg-surface-900 rounded-lg overflow-hidden"
  >
    <div class="px-6 pt-4">
      <Button
        label="Back to Clinics"
        icon="pi pi-arrow-left"
        text
        class="!p-0 text-surface-500 hover:text-surface-900"
        @click="$router.push('/superadmin/clinics')"
      />
    </div>

    <div v-if="isLoading" class="p-6 space-y-6">
      <div class="flex gap-4 items-center">
        <Skeleton shape="circle" size="4rem" />
        <div class="flex-1">
          <Skeleton width="12rem" class="mb-2" />
          <Skeleton width="8rem" />
        </div>
      </div>
      <Skeleton height="12rem" class="rounded-xl" />
    </div>

    <div v-else-if="isError" class="p-6 text-center">
      <div class="text-red-500 mb-2"><i class="pi pi-exclamation-circle text-2xl"></i></div>
      <h3 class="text-lg font-bold">Could not load clinic</h3>
      <Button label="Retry" text @click="refetch()" />
    </div>

    <div v-else-if="stats" class="flex-1 flex flex-col animate-fade-in">
      <div
        class="px-6 py-6 bg-surface-50 dark:bg-surface-900 border-b border-surface-200 dark:border-surface-800"
      >
        <div class="flex flex-col md:flex-row justify-between items-start gap-4">
          <div class="flex gap-4">
            <Avatar
              :image="stats.logoUrl"
              :label="!stats.logoUrl ? stats.name.charAt(0) : undefined"
              size="xlarge"
              shape="circle"
              class="!w-16 !h-16 !text-2xl bg-primary text-white shadow-sm border-2 border-white dark:border-surface-800"
            />
            <div>
              <h1 class="text-2xl font-bold text-surface-900 dark:text-surface-0">
                {{ stats.name }}
              </h1>
              <div class="flex items-center gap-2 mt-1 text-surface-500 text-sm">
                <i class="pi pi-globe"></i>
                <a
                  :href="`https://${stats.identifier}.nabdcare.com`"
                  target="_blank"
                  class="hover:underline font-mono"
                >
                  {{ stats.identifier }}.nabdcare.com
                </a>
                <Tag
                  :value="stats.isActive ? 'Active' : 'Suspended'"
                  :severity="stats.isActive ? 'success' : 'danger'"
                  class="ml-2"
                />
              </div>
            </div>
          </div>

          <div class="flex gap-2">
            <Button label="Impersonate" icon="pi pi-user-edit" severity="secondary" outlined />
            <Button icon="pi pi-cog" severity="secondary" text rounded />
          </div>
        </div>

        <div class="flex gap-1 mt-8 overflow-x-auto">
          <router-link
            v-for="tab in tabs"
            :key="tab.route"
            :to="{ name: tab.route, params: { id: clinicId } }"
            active-class="!border-primary !text-primary bg-primary-50/50 dark:bg-primary-900/20"
            class="px-4 py-2 text-sm font-medium rounded-t-lg border-b-2 border-transparent transition-colors whitespace-nowrap flex items-center gap-2 text-surface-500 hover:text-surface-700 hover:border-surface-300"
          >
            <i :class="tab.icon"></i>
            {{ tab.label }}
          </router-link>
        </div>
      </div>

      <div class="p-6 flex-1 bg-surface-50 dark:bg-surface-900">
        <router-view :stats="stats" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { useClinicQueries } from '@/composables/query/clinics/useClinicQueries';
  import Avatar from 'primevue/avatar';
  import Button from 'primevue/button';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';
  import { useRoute } from 'vue-router';

  const route = useRoute();
  const clinicId = computed(() => route.params.id as string);

  const { useClinicStats } = useClinicQueries();
  const { data: stats, isLoading, isError, refetch } = useClinicStats(clinicId);

  const tabs = [
    { label: 'Overview', route: 'clinic-overview', icon: 'pi pi-home' },
    { label: 'Subscription', route: 'clinic-subscription', icon: 'pi pi-wallet' },
  ];
</script>
