<script setup lang="ts">
  import { computed } from 'vue';
  import {
    useActiveSubscription,
    useSubscriptionPlans,
  } from '@/composables/query/subscriptions/useSubscriptions';

  // Custom Components
  import BillingCycleCard from '@/components/Clinic/profile/Subscription/BillingCycleCard.vue';
  import BranchesCard from '@/components/Clinic/profile/Subscription/BranchesCard.vue';
  import SubscriptionBreakdown from '@/components/Clinic/profile/Subscription/SubscriptionBreakdown.vue';
  import SubscriptionHeader from '@/components/Clinic/profile/Subscription/SubscriptionHeader.vue';
  import UsersCard from '@/components/Clinic/profile/Subscription/UsersCard.vue';
  import InvoiceList from '@/components/Invoices/InvoiceList.vue';
  import Button from 'primevue/button';
  import Skeleton from 'primevue/skeleton';
  import BaseCard from '@/components/shared/BaseCard.vue';

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
    <Skeleton height="100px" borderRadius="12px" class="bg-surface-0 dark:bg-surface-900" />
    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
      <Skeleton
        height="400px"
        borderRadius="12px"
        class="xl:col-span-2 bg-surface-0 dark:bg-surface-900"
      />
      <Skeleton
        height="400px"
        borderRadius="12px"
        class="xl:col-span-1 bg-surface-0 dark:bg-surface-900"
      />
    </div>
  </div>

  <div
    v-else-if="!activeSub"
    class="text-center p-10 bg-surface-0 dark:bg-surface-900 rounded-xl border border-dashed border-surface-300 dark:border-surface-700"
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

    <BaseCard v-if="clinicId" no-padding class="mt-6 [&_.p-datatable-thead_th]:!text-xs">
      <div class="rounded-b-lg overflow-hidden mb-0.1">
        <InvoiceList :clinic-id="clinicId" :subscription-id="activeSub?.id" @view-all="" />
      </div>
    </BaseCard>
  </div>
</template>
