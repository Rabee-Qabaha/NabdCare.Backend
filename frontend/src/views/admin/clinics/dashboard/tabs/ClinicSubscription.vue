<script setup lang="ts">
  import { computed } from 'vue';

  import {
    useActiveSubscription,
    useSubscriptionPlans,
  } from '@/composables/query/subscriptions/useSubscriptions';

  // Custom Components
  import BillingCycleCard from '@/components/Subscription/profile/Subscription/BillingCycleCard.vue';
  import BranchesCard from '@/components/Subscription/profile/Subscription/BranchesCard.vue';
  import SubscriptionBreakdown from '@/components/Subscription/profile/Subscription/SubscriptionBreakdown.vue';
  import SubscriptionHeader from '@/components/Subscription/profile/Subscription/SubscriptionHeader.vue';
  import UsersCard from '@/components/Subscription/profile/Subscription/UsersCard.vue';
  import InvoiceHistory from '@/components/Subscription/InvoiceHistory.vue';
  import Button from 'primevue/button';
  import Skeleton from 'primevue/skeleton';

  const props = defineProps<{
    clinicId: string;
  }>();

  // -- Data Fetching --
  const { data: activeSubData, isLoading: isLoadingActive } = useActiveSubscription(
    computed(() => props.clinicId),
  );

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
    <SubscriptionHeader
      :subscription="activeSub"
      :clinic-id="clinicId"
      :plan-definition="activePlanDef"
    />

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
