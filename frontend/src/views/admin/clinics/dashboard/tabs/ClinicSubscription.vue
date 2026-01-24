<script setup lang="ts">
  import { computed, ref } from 'vue';
  import { useRoute } from 'vue-router';

  // PrimeVue Imports
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  // Custom Components
  import BillingCycleCard from '@/components/Subscription/profile/Subscription/BillingCycleCard.vue';
  import BranchesCard from '@/components/Subscription/profile/Subscription/BranchesCard.vue';
  import SubscriptionBreakdown from '@/components/Subscription/profile/Subscription/SubscriptionBreakdown.vue';
  import UsersCard from '@/components/Subscription/profile/Subscription/UsersCard.vue';

  // API & Composables
  import {
    useActiveSubscription,
    useSubscriptionPlans,
  } from '@/composables/query/subscriptions/useSubscriptions';
  import { InvoiceStatus } from '@/types/backend/invoice-status';

  const route = useRoute();
  const clinicId = computed(() => route.params.id as string);

  // -- Data Fetching --
  const { data: activeSubData, isLoading: isLoadingActive } = useActiveSubscription(clinicId);

  const { data: availablePlans } = useSubscriptionPlans();

  // -- Computed --
  const activeSub = computed(() => {
    const raw = activeSubData.value;
    // Handle the structure returned by getActiveForClinic
    if (!raw || !raw.subscription) return null;
    return raw.subscription;
  });

  const activePlanDef = computed(
    () => availablePlans.value?.find((p) => p.id === activeSub.value?.planId) || null,
  );

  const daysRemaining = computed(() => {
    if (!activeSub.value?.endDate) return 0;
    const end = new Date(activeSub.value.endDate).getTime();
    const now = new Date().getTime();
    const diffInMs = end - now;
    const diffInDays = diffInMs / (1000 * 60 * 60 * 24);
    return Math.max(0, Math.ceil(diffInDays));
  });

  const history = ref([
    { id: '#INV-2023-009', billingPeriod: 'Sep 24 - Oct 23, 2023', amount: 1200.0, status: 'Paid' },
    { id: '#INV-2023-008', billingPeriod: 'Aug 24 - Sep 23, 2023', amount: 1050.0, status: 'Paid' },
    { id: '#INV-2023-007', billingPeriod: 'Jul 24 - Aug 23, 2023', amount: 1050.0, status: 'Paid' },
  ]);

  const formatMoney = (val: number) => {
    if (!activeSub.value) return '$0.00';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: activeSub.value.currency || 'USD',
    }).format(val);
  };

  const isPaymentActionRequired = computed(() => {
    const status = activeSub.value?.latestInvoiceStatus;
    return (
      status === InvoiceStatus.Overdue ||
      status === InvoiceStatus.PartiallyPaid ||
      status === InvoiceStatus.Issued
    );
  });
</script>

<template>
  <div
    class="w-full max-w-[1600px] mx-auto p-6 md:p-8 space-y-6 bg-surface-50 dark:bg-transparent rounded-2xl font-sans"
  >
    <div v-if="isLoadingActive" class="space-y-6">
      <Skeleton height="100px" borderRadius="12px" class="bg-surface-0 dark:bg-[#27272a]" />
      <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <Skeleton
          height="400px"
          borderRadius="12px"
          class="xl:col-span-2 bg-surface-0 dark:bg-[#27272a]"
        />
        <Skeleton
          height="400px"
          borderRadius="12px"
          class="xl:col-span-1 bg-surface-0 dark:bg-[#27272a]"
        />
      </div>
    </div>

    <div
      v-else-if="!activeSub"
      class="text-center p-10 bg-surface-0 dark:bg-[#27272a] rounded-xl border border-dashed border-surface-300 dark:border-surface-700"
    >
      <i class="pi pi-box text-4xl text-surface-400 mb-4"></i>
      <h3 class="text-xl font-bold text-surface-900 dark:text-surface-0">No Active Subscription</h3>
      <p class="text-surface-500 mb-6">This clinic does not have a subscription plan assigned.</p>
      <Button label="Assign Plan" icon="pi pi-plus" />
    </div>

    <div v-else>
      <div
        class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-5 border border-transparent dark:border-surface-700 shadow dark:shadow-sm flex flex-col md:flex-row justify-between items-center gap-4 transition-colors duration-300 mb-6"
      >
        <div class="flex items-center gap-4 w-full md:w-auto">
          <div
            class="w-12 h-12 rounded-lg bg-primary-50 dark:bg-primary-900/20 text-primary-600 flex items-center justify-center shadow-sm"
          >
            <i class="pi pi-star-fill text-xl"></i>
          </div>
          <div>
            <div class="flex items-center gap-3">
              <h2 class="text-xl font-bold text-surface-900 dark:text-surface-0">
                {{ activePlanDef?.name || activeSub.planId }}
              </h2>
              <Tag
                value="ACTIVE"
                severity="success"
                class="!px-2 !py-0.5 !text-[10px] !font-bold"
                rounded
              />
            </div>
            <div
              class="flex items-center gap-2 text-surface-500 dark:text-surface-400 text-sm font-medium mt-1"
            >
              <i class="pi pi-calendar"></i>
              <span>
                Renewal in
                <span class="text-surface-900 dark:text-surface-0 font-bold">
                  {{ daysRemaining }} Days
                </span>
              </span>
            </div>
          </div>
        </div>

        <div class="flex flex-col sm:flex-row items-center gap-3 w-full md:w-auto">
          <div
            v-if="isPaymentActionRequired"
            class="flex items-center gap-2 px-4 py-2.5 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-lg border border-red-100 dark:border-red-800 text-sm font-bold w-full sm:w-auto justify-center"
          >
            <i class="pi pi-exclamation-triangle"></i>
            <span>Payment Action Required</span>
          </div>

          <Button
            label="Manage Subscription"
            icon="pi pi-cog"
            class="!bg-emerald-500 !border-emerald-500 hover:!bg-emerald-600 !rounded-lg !font-bold w-full sm:w-auto"
          />
        </div>
      </div>

      <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div class="xl:col-span-2 flex flex-col gap-6">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <BranchesCard :subscription="activeSub" :clinic-id="clinicId!" />
            <UsersCard :subscription="activeSub" :clinic-id="clinicId!" />
          </div>

          <BillingCycleCard :subscription="activeSub" :clinic-id="clinicId!" />
        </div>

        <div class="xl:col-span-1 h-full">
          <SubscriptionBreakdown :subscription="activeSub" />
        </div>
      </div>

      <div class="mt-6">
        <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 mb-4 px-1">
          Billing History
        </h3>
        <div
          class="bg-surface-0 dark:bg-[#27272a] rounded-xl border border-transparent dark:border-surface-700 shadow dark:shadow-sm overflow-hidden transition-colors duration-300"
        >
          <DataTable :value="history" class="text-sm">
            <Column
              field="id"
              header="INVOICE ID"
              class="font-medium text-surface-900 dark:text-surface-0"
            ></Column>
            <Column
              field="billingPeriod"
              header="BILLING PERIOD"
              class="text-surface-500 dark:text-surface-400"
            ></Column>
            <Column field="amount" header="AMOUNT">
              <template #body="{ data }">
                <span class="font-bold text-primary-600 dark:text-primary-400">
                  {{ formatMoney(data.amount) }}
                </span>
              </template>
            </Column>
            <Column field="status" header="STATUS">
              <template #body="{ data }">
                <Tag
                  :value="data.status"
                  severity="success"
                  class="!uppercase !text-[10px] !px-2.5"
                  rounded
                />
              </template>
            </Column>
            <Column header="ACTIONS" class="text-right">
              <template #body>
                <Button
                  icon="pi pi-download"
                  text
                  rounded
                  severity="secondary"
                  class="!w-8 !h-8 !text-surface-400 hover:!text-surface-600 dark:hover:!text-surface-200"
                />
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
  :deep(.p-datatable .p-datatable-thead > tr > th) {
    background: transparent;
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--surface-400);
    font-weight: 700;
    padding-top: 1.5rem;
    padding-bottom: 0.75rem;
  }
  :deep(.p-datatable .p-datatable-tbody > tr > td) {
    padding-top: 1.25rem;
    padding-bottom: 1.25rem;
    border-bottom: 1px solid var(--surface-100);
  }
  :global(.dark) :deep(.p-datatable .p-datatable-tbody > tr > td) {
    border-bottom: 1px solid var(--surface-800);
  }
  :deep(.p-datatable .p-datatable-tbody > tr:last-child > td) {
    border-bottom: none;
  }
</style>
