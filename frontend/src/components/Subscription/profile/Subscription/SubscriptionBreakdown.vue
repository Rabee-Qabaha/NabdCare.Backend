<script setup lang="ts">
  import { useSubscriptionPlans } from '@/composables/query/subscriptions/useSubscriptions';
  import { type SubscriptionResponseDto, SubscriptionType } from '@/types/backend';
  import { formatCurrency, formatDate } from '@/utils/uiHelpers';
  import Divider from 'primevue/divider';
  import { computed } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto }>();
  const { data: availablePlans } = useSubscriptionPlans();

  // --- Computed Data ---

  const activePlanDef = computed(
    () => availablePlans.value?.find((p) => p.id === props.subscription.planId) || null,
  );

  const isTrial = computed(() => (props.subscription.planId || '').toUpperCase().includes('TRIAL'));

  // Counts
  const paidUsers = computed(() => props.subscription.purchasedUsers || 0);
  const bonusUsers = computed(() => props.subscription.bonusUsers || 0);

  const paidBranches = computed(() => props.subscription.purchasedBranches || 0);
  const bonusBranches = computed(() => props.subscription.bonusBranches || 0);

  // Helper for Billing Cycle Logic
  const isYearly = computed(() => props.subscription.type === SubscriptionType.Yearly);

  // Costs (Fallback logic if PlanDefinition isn't loaded yet)
  // Monthly: Users $10, Branches $20
  // Yearly: Users $100, Branches $150
  const userPrice = computed(() => activePlanDef.value?.userPrice ?? (isYearly.value ? 100 : 10));
  const branchPrice = computed(
    () => activePlanDef.value?.branchPrice ?? (isYearly.value ? 150 : 20),
  );
  const basePlanFee = computed(() => activePlanDef.value?.baseFee ?? 0);

  const basePrice = computed(() => (isTrial.value ? 0 : basePlanFee.value));

  // Total line item costs
  const userTotalCost = computed(() => (isTrial.value ? 0 : paidUsers.value * userPrice.value));
  const branchTotalCost = computed(() =>
    isTrial.value ? 0 : paidBranches.value * branchPrice.value,
  );

  const currency = computed(() => props.subscription.currency || 'USD');

  // Updated Billing Cycle Text using the Enum
  const billingCycle = computed(() => {
    return props.subscription.type || SubscriptionType.Monthly;
  });
</script>

<template>
  <div
    class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-6 border border-transparent dark:border-surface-700 shadow dark:shadow-sm h-full flex flex-col transition-colors duration-300"
  >
    <h3
      class="text-xs font-bold text-surface-400 dark:text-surface-500 uppercase tracking-widest mb-2"
    >
      Subscription Breakdown
    </h3>

    <div class="flex items-start justify-between mb-8">
      <div>
        <div
          class="text-xl font-extrabold text-surface-900 dark:text-surface-0 leading-tight uppercase"
        >
          {{ activePlanDef?.name || subscription.planId }}
        </div>
        <div class="text-sm text-surface-500 dark:text-surface-400 font-medium mt-1">
          Billed {{ billingCycle }}
        </div>
      </div>
      <div
        class="p-2 bg-surface-100 dark:bg-surface-800 rounded-lg text-surface-500 dark:text-surface-400"
      >
        <i class="pi pi-receipt text-xl"></i>
      </div>
    </div>

    <ul class="space-y-6 flex-1">
      <li class="flex justify-between items-start group">
        <div class="flex gap-3">
          <div
            class="w-9 h-9 rounded-lg bg-surface-50 dark:bg-surface-800 flex items-center justify-center text-surface-500 shrink-0"
          >
            <i class="pi pi-box"></i>
          </div>
          <div>
            <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">
              Base Subscription
            </div>
            <div class="text-xs text-surface-500 mt-0.5">Plan core features</div>
          </div>
        </div>
        <div class="font-bold text-surface-900 dark:text-surface-0">
          {{ formatCurrency(basePrice, currency) }}
        </div>
      </li>

      <li class="flex justify-between items-start group">
        <div class="flex gap-3">
          <div
            class="w-9 h-9 rounded-lg bg-primary-50 dark:bg-primary-900/20 flex items-center justify-center text-primary-500 shrink-0"
          >
            <i class="pi pi-users"></i>
          </div>
          <div>
            <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">Add-on Users</div>
            <div class="text-xs text-surface-500 mt-0.5">
              ({{ paidUsers }} Paid / {{ bonusUsers }} Free)
            </div>
          </div>
        </div>
        <div class="font-bold text-surface-900 dark:text-surface-0">
          {{ formatCurrency(userTotalCost, currency) }}
        </div>
      </li>

      <li class="flex justify-between items-start group">
        <div class="flex gap-3">
          <div
            class="w-9 h-9 rounded-lg bg-orange-50 dark:bg-orange-900/20 flex items-center justify-center text-orange-500 shrink-0"
          >
            <i class="pi pi-building"></i>
          </div>
          <div>
            <div class="font-bold text-surface-900 dark:text-surface-0 text-sm">
              Add-on Branches
            </div>
            <div class="text-xs text-surface-500 mt-0.5">
              ({{ paidBranches }} Paid / {{ bonusBranches }} Free)
            </div>
          </div>
        </div>
        <div class="font-bold text-surface-900 dark:text-surface-0">
          {{ formatCurrency(branchTotalCost, currency) }}
        </div>
      </li>
    </ul>

    <Divider class="my-6 border-dashed !border-surface-200 dark:!border-surface-700" />

    <div
      class="bg-primary-50 dark:bg-primary-900/10 rounded-xl p-5 border border-primary-100 dark:border-primary-800/30"
    >
      <div class="flex justify-between items-center mb-1">
        <span
          class="text-sm font-bold text-primary-600/70 dark:text-primary-400/70 uppercase tracking-wider"
        >
          Total {{ billingCycle }} Fee
        </span>
      </div>
      <div class="flex justify-between items-baseline">
        <span class="text-xs font-medium text-surface-500 dark:text-surface-400">
          Next bill {{ formatDate(subscription.endDate) }}
        </span>
        <span class="text-2xl font-bold text-primary-600 dark:text-primary-400">
          {{ formatCurrency(subscription.fee, currency) }}
        </span>
      </div>
    </div>
  </div>
</template>
