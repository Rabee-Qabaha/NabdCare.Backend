<script setup lang="ts">
  import InvoiceList from '@/components/Invoices/InvoiceList.vue';
  import Button from 'primevue/button';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Tag from 'primevue/tag';
  import { ref } from 'vue';

  const props = defineProps<{
    clinicId: string;
  }>();

  const invoiceListRef = ref<InstanceType<typeof InvoiceList> | null>(null);
  const totalInvoices = ref(0);
  const searchQuery = ref('');
  const showFilters = ref(false);

  // Actions
  const handleRefetch = () => {
    invoiceListRef.value?.refetch();
  };

  const clearFilters = () => {
    searchQuery.value = '';
    invoiceListRef.value?.clearFilters();
  };
</script>

<template>
  <div class="card p-0 border-0 shadow-none h-full">
    <!-- Header / Toolbar -->
    <div
      class="flex flex-col gap-3 rounded-xl border border-surface-200 bg-white p-3 shadow-sm dark:border-surface-700 dark:bg-surface-900 md:flex-row md:items-center md:justify-between mb-4"
    >
      <div class="flex flex-wrap gap-2">
        <!-- Add Invoice Button could go here -->
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <div class="flex items-center gap-2 mr-2">
          <span class="text-surface-500 font-medium text-sm">Total:</span>
          <Tag :value="totalInvoices" severity="secondary" rounded />
        </div>

        <!-- Filter Button -->
        <Button
          label="Filters"
          icon="pi pi-sliders-h"
          severity="secondary"
          outlined
          class="w-full sm:w-auto"
          @click="showFilters = true"
        />

        <div class="mx-1 hidden h-6 w-px bg-surface-200 dark:bg-surface-700 md:block"></div>

        <div class="flex w-full justify-end gap-1 sm:w-auto">
          <Button
            v-tooltip.top="'Reset filters'"
            icon="pi pi-filter-slash"
            severity="secondary"
            text
            rounded
            @click="clearFilters"
          />

          <Button
            v-tooltip.top="'Refresh'"
            icon="pi pi-refresh"
            severity="secondary"
            text
            rounded
            @click="handleRefetch"
          />
        </div>
      </div>
    </div>

    <!-- Search Bar -->
    <div class="mb-4">
      <IconField class="w-full">
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="searchQuery"
          placeholder="Search invoices by number..."
          class="w-full"
        />
      </IconField>
    </div>

    <!-- Table Container -->
    <div>
      <InvoiceList
        ref="invoiceListRef"
        :clinic-id="props.clinicId"
        :show-allocations="true"
        layout="list"
        :search="searchQuery"
        @update:total-records="totalInvoices = $event"
      />
    </div>
  </div>
</template>
