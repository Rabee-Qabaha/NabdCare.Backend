<template>
  <div class="card p-0 border-0 shadow-none h-full flex flex-col">
    <!-- Header / Toolbar -->
    <div
      class="flex flex-col gap-3 rounded-xl border border-surface-200 bg-white p-3 shadow-sm dark:border-surface-700 dark:bg-surface-900 md:flex-row md:items-center md:justify-between mb-4"
    >
      <div class="flex flex-wrap gap-2">
        <Button
          v-if="canCreate"
          label="Add Payment"
          icon="pi pi-plus"
          class="w-full font-semibold shadow-sm sm:w-auto"
          @click="paymentListRef?.openCreateDialog()"
        />
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <div class="flex items-center gap-2 mr-2">
          <span class="text-surface-500 font-medium text-sm">Total:</span>
          <Tag :value="totalRecords" severity="secondary" rounded />
        </div>

        <!-- Filter Button -->
        <Button
          label="Filters"
          icon="pi pi-sliders-h"
          severity="secondary"
          outlined
          class="w-full sm:w-auto"
          @click="paymentListRef?.openFilterDialog()"
        />

        <div class="mx-1 hidden h-6 w-px bg-surface-200 dark:bg-surface-700 md:block"></div>

        <div class="flex w-full justify-end gap-1 sm:w-auto">
          <Button
            v-tooltip.top="'Reset filters'"
            icon="pi pi-filter-slash"
            severity="secondary"
            text
            rounded
            @click="handleClearFilters"
          />

          <Button
            v-tooltip.top="'Refresh'"
            icon="pi pi-refresh"
            severity="secondary"
            text
            rounded
            :loading="loading"
            @click="paymentListRef?.refresh()"
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
          placeholder="Search payments by transaction ID, method or status..."
          class="w-full"
        />
      </IconField>
    </div>

    <!-- Payment List Component -->
    <PaymentList
      class="flex-grow min-h-0"
      ref="paymentListRef"
      :clinic-id="clinicId"
      :search="searchQuery"
      @update:total-records="totalRecords = $event"
      @update:loading="loading = $event"
    />
  </div>
</template>

<script setup lang="ts">
  import PaymentList from '@/components/Payments/PaymentList.vue';
  import { usePaymentActions } from '@/composables/query/payments/usePaymentActions.ts';
  import Button from 'primevue/button';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Tag from 'primevue/tag';
  import { computed, ref } from 'vue';
  import { useRoute } from 'vue-router';

  const route = useRoute();
  const clinicId = computed(() => route.params.id as string);

  const paymentListRef = ref<InstanceType<typeof PaymentList> | null>(null);
  const totalRecords = ref(0);
  const loading = ref(false);
  const searchQuery = ref('');

  const { canCreate } = usePaymentActions();

  const handleClearFilters = () => {
    searchQuery.value = '';
    paymentListRef.value?.clearFilters();
  };
</script>
