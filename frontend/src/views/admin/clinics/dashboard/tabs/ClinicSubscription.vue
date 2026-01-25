<script setup lang="ts">
  import { computed } from 'vue';

  // PrimeVue Imports
  import Button from 'primevue/button';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  // Custom Components
  import BillingCycleCard from '@/components/Subscription/profile/Subscription/BillingCycleCard.vue';
  import BranchesCard from '@/components/Subscription/profile/Subscription/BranchesCard.vue';
  import SubscriptionBreakdown from '@/components/Subscription/profile/Subscription/SubscriptionBreakdown.vue';
  import UsersCard from '@/components/Subscription/profile/Subscription/UsersCard.vue';
  import BaseCard from '@/components/shared/BaseCard.vue';
  import InvoiceHistory from '@/components/Subscription/InvoiceHistory.vue';

  // API & Composables
  import { useInfiniteInvoicesPaged } from '@/composables/query/invoices/useInvoices';
  import {
    useActiveSubscription,
    useSubscriptionPlans,
  } from '@/composables/query/subscriptions/useSubscriptions';
  import { InvoiceStatus, SubscriptionStatus } from '@/types/backend';

  const props = defineProps<{
    clinicId: string;
  }>();

  // -- Data Fetching --
  const { data: activeSubData, isLoading: isLoadingActive } = useActiveSubscription(
    computed(() => props.clinicId),
  );

  // 🛡️ Safety Check: Fetch any overdue invoices (even if latest is paid)
  const { data: overdueInvoicesData } = useInfiniteInvoicesPaged({
    clinicId: computed(() => props.clinicId),
    status: InvoiceStatus.Overdue,
    limit: 1,
  });

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

  const hasOverdueInvoices = computed(() => {
    const pages = overdueInvoicesData.value?.pages;
    return pages?.[0]?.items?.length ? pages[0].items.length > 0 : false;
  });

  const isPaymentActionRequired = computed(() => {
    const sub = activeSub.value;
    if (!sub) return false;

    // 1. Check Subscription Status (Primary)
    if (sub.status === SubscriptionStatus.PastDue || sub.status === SubscriptionStatus.Suspended) {
      return true;
    }

    // 2. Check for ANY Overdue Invoices (Safety Net)
    if (hasOverdueInvoices.value) {
      return true;
    }

    // 3. Check Latest Invoice Status (Backend Provided)
    const status = sub.latestInvoiceStatus;
    if (status) {
      return (
        status === InvoiceStatus.Overdue ||
        status === InvoiceStatus.PartiallyPaid ||
        status === InvoiceStatus.Issued
      );
    }

    return false;
  });

  const paymentStatusLabel = computed(() => {
    const sub = activeSub.value;
    if (sub?.status === SubscriptionStatus.Suspended) return 'Subscription Suspended';
    if (sub?.status === SubscriptionStatus.PastDue) return 'Payment Past Due';

    if (hasOverdueInvoices.value) return 'Unpaid Invoices';

    const status = sub?.latestInvoiceStatus;
    if (!status) return 'Action Required';

    if (status === InvoiceStatus.Overdue) return 'Invoice Overdue';
    if (status === InvoiceStatus.PartiallyPaid) return 'Balance Due';
    if (status === InvoiceStatus.Issued) return 'Payment Required';

    return 'Action Required';
  });
</script>

<template>
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
    <BaseCard class="p-5 flex flex-col md:flex-row justify-between items-center gap-4 mb-6">
      <div class="flex items-center gap-4 w-full md:w-auto">
        <div
          class="w-12 h-12 rounded-lg bg-primary-100 dark:bg-primary-400/10 text-primary-600 dark:text-primary-400 flex items-center justify-center shadow-sm"
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

          <div class="flex items-center gap-2 text-muted-color text-sm font-medium mt-1">
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
          class="relative flex items-center gap-3 pl-3 pr-5 py-1.5 bg-gradient-to-r from-red-50 to-orange-50/50 dark:from-red-900/20 dark:to-orange-900/10 rounded-lg border border-red-200/60 dark:border-red-500/20 backdrop-blur-sm shadow-sm transition-all hover:shadow-md hover:border-red-300 dark:hover:border-red-500/30 group cursor-default"
        >
          <!-- Status Indicator Dot -->
          <span class="relative flex h-2.5 w-2.5 shrink-0">
            <span
              class="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"
            ></span>
            <span class="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500"></span>
          </span>

          <div class="flex flex-col">
            <span
              class="text-[9px] uppercase font-bold text-red-500/70 leading-tight tracking-wider"
            >
              Action Required
            </span>
            <span
              class="text-xs font-bold text-red-700 dark:text-red-400 leading-tight group-hover:text-red-800 dark:group-hover:text-red-300 transition-colors"
            >
              {{ paymentStatusLabel }}
            </span>
          </div>
        </div>

        <Button
          label="Manage Subscription"
          icon="pi pi-cog"
          class="!bg-emerald-500 !border-emerald-500 hover:!bg-emerald-600 !rounded-lg !font-bold w-full sm:w-auto"
        />
      </div>
    </BaseCard>

    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
      <div class="xl:col-span-2 flex flex-col gap-6">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <BranchesCard :subscription="activeSub" :clinic-id="clinicId" />
          <UsersCard :subscription="activeSub" :clinic-id="clinicId" />
        </div>

        <BillingCycleCard :subscription="activeSub" :clinic-id="clinicId" />
      </div>

      <div class="xl:col-span-1 h-full">
        <SubscriptionBreakdown :subscription="activeSub" />
      </div>
    </div>

    <div class="mt-6" v-if="clinicId">
      <InvoiceHistory :clinic-id="clinicId" @view-all="" />
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
