<template>
  <div class="flex flex-col min-h-screen bg-surface-50 dark:bg-surface-800">
    <div
      class="bg-surface-0 dark:bg-surface-900 border-b border-surface-200 dark:border-surface-800"
    >
      <div class="px-6 pt-6">
        <Button
          label="Back to Clinics"
          icon="pi pi-arrow-left"
          link
          class="!p-0 !text-primary-600 hover:!text-primary-700 !font-medium"
          @click="$router.push('/superadmin/clinics')"
        />
      </div>

      <div v-if="isLoading" class="p-6 space-y-6">
        <div class="flex gap-4 items-center">
          <Skeleton shape="square" size="4rem" borderRadius="12px" class="shrink-0" />
          <div class="flex-1 space-y-2">
            <Skeleton width="40%" height="2rem" />
            <Skeleton width="20%" height="1rem" />
          </div>
        </div>
      </div>

      <div v-else-if="isError" class="p-6 text-center">
        <div class="text-red-500 mb-2"><i class="pi pi-exclamation-circle text-2xl"></i></div>
        <h3 class="text-lg font-bold">Could not load clinic</h3>
        <Button label="Retry" text @click="refetch()" />
      </div>

      <div v-else-if="stats" class="px-6 pt-5 animate-fade-in">
        <div class="flex flex-col md:flex-row justify-between items-start gap-4 mb-8">
          <div class="flex gap-5 items-center">
            <div
              class="w-16 h-16 rounded-2xl bg-surface-100 dark:bg-surface-800 flex items-center justify-center text-2xl font-bold text-surface-600 dark:text-surface-300 border border-surface-100 dark:border-surface-700 shrink-0 overflow-hidden"
            >
              <img
                v-if="stats.logoUrl"
                :src="stats.logoUrl"
                class="w-full h-full object-cover"
                alt="Clinic Logo"
              />
              <span v-else>{{ stats.name.charAt(0) }}</span>
            </div>

            <div>
              <div class="flex items-center gap-3">
                <h1 class="text-2xl font-bold text-surface-900 dark:text-surface-0 leading-tight">
                  {{ stats.name }}
                </h1>
                <Tag
                  :value="stats.isActive ? 'ACTIVE' : 'SUSPENDED'"
                  :severity="stats.isActive ? 'success' : 'danger'"
                  class="!text-[10px] !font-bold !px-2.5 !py-0.5 rounded uppercase tracking-wide"
                />
              </div>
              <div class="flex items-center gap-2 mt-1.5 text-surface-500 text-sm">
                <i class="pi pi-globe text-xs"></i>
                <a
                  :href="`https://${stats.identifier}.nabdcare.com`"
                  target="_blank"
                  class="hover:text-primary-600 hover:underline transition-colors"
                >
                  {{ stats.identifier }}.nabdcare.com
                </a>
              </div>
            </div>
          </div>

          <div class="flex gap-3">
            <Button
              label="Impersonate"
              icon="pi pi-user-edit"
              severity="secondary"
              outlined
              class="!bg-surface-0 dark:!bg-surface-800 !border-surface-300 dark:!border-surface-700 !text-surface-700 dark:!text-surface-200 hover:!bg-surface-50 dark:hover:!bg-surface-700 !rounded-lg !font-medium"
            />
            <Button
              icon="pi pi-cog"
              severity="secondary"
              outlined
              class="!bg-surface-0 dark:!bg-surface-800 !border-surface-300 dark:!border-surface-700 !text-surface-700 dark:!text-surface-200 hover:!bg-surface-50 dark:hover:!bg-surface-700 !rounded-lg !w-10 !h-10"
            />
          </div>
        </div>

        <TabMenu
          :model="tabItems"
          v-model:activeIndex="activeTabIndex"
          class="custom-clinic-tabs"
        />
      </div>
    </div>

    <div v-if="stats" class="p-6 flex-1">
      <router-view :stats="stats" :clinic-id="id" />
    </div>
  </div>
</template>

<script setup lang="ts">
  import { useClinicQueries } from '@/composables/query/clinics/useClinicQueries';
  import Button from 'primevue/button';
  import Skeleton from 'primevue/skeleton';
  import TabMenu from 'primevue/tabmenu';
  import Tag from 'primevue/tag';
  import { computed, ref, watch } from 'vue';
  import { useRoute, useRouter } from 'vue-router';

  const props = defineProps<{
    id: string;
  }>();

  const router = useRouter();
  const route = useRoute();

  // Data Fetching
  const { useClinicStats } = useClinicQueries();
  const { data: stats, isLoading, isError, refetch } = useClinicStats(computed(() => props.id));

  // Tab Logic
  const tabItems = ref([
    {
      label: 'Overview',
      icon: 'pi pi-th-large',
      command: () => router.push({ name: 'clinic-overview', params: { id: props.id } }),
    },
    {
      label: 'Subscription',
      icon: 'pi pi-desktop',
      command: () => router.push({ name: 'clinic-subscription', params: { id: props.id } }),
    },
    {
      label: 'Branches',
      icon: 'pi pi-building',
      command: () => router.push({ name: 'clinic-branches', params: { id: props.id } }),
    },
    {
      label: 'Payments',
      icon: 'pi pi-wallet',
      command: () => router.push({ name: 'clinic-payments', params: { id: props.id } }),
    },
  ]);

  const activeTabIndex = ref(0);

  // Sync tab state with current route
  watch(
    () => route.name,
    (newRouteName) => {
      if (newRouteName === 'clinic-subscription') {
        activeTabIndex.value = 1;
      } else if (newRouteName === 'clinic-branches') {
        activeTabIndex.value = 2;
      } else if (newRouteName === 'clinic-payments') {
        activeTabIndex.value = 3;
      } else {
        activeTabIndex.value = 0;
      }
    },
    { immediate: true },
  );
</script>

<style scoped>
  /* * Custom Override for PrimeVue TabMenu to match the design 
   * This creates the transparent background and green underline style
   */
  :deep(.custom-clinic-tabs .p-tabmenu-nav) {
    background: transparent;
    border-bottom: none; /* Removed the full-width border to match cleaner look */
  }

  :deep(.custom-clinic-tabs .p-tabmenuitem) {
    background: transparent;
    margin-right: 1.5rem;
  }

  :deep(.custom-clinic-tabs .p-menuitem-link) {
    background: transparent !important;
    border: none !important;
    border-bottom: 2px solid transparent !important;
    padding: 1rem 0.5rem;
    color: var(--surface-500);
    font-weight: 600;
    transition: all 0.2s;
    box-shadow: none !important;
  }

  /* Hover State */
  :deep(.custom-clinic-tabs .p-menuitem-link:hover) {
    color: var(--surface-900);
    border-color: transparent;
  }

  /* Active State (Green Highlight) */
  :deep(.custom-clinic-tabs .p-tabmenuitem.p-highlight .p-menuitem-link) {
    color: var(--primary-500) !important; /* Uses your theme's primary green */
    border-color: var(--primary-500) !important;
  }

  /* Icon spacing */
  :deep(.custom-clinic-tabs .p-menuitem-icon) {
    margin-right: 0.5rem;
  }

  /* Dark Mode Tweaks */
  :global(.dark) :deep(.custom-clinic-tabs .p-menuitem-link) {
    color: var(--surface-400);
  }
  :global(.dark) :deep(.custom-clinic-tabs .p-menuitem-link:hover) {
    color: var(--surface-200);
  }
  :global(.dark) :deep(.custom-clinic-tabs .p-tabmenuitem.p-highlight .p-menuitem-link) {
    color: var(--primary-400) !important;
    border-color: var(--primary-400) !important;
  }
</style>
