// src/components/Subscription/CurrentPlanCard.vue
<script setup lang="ts">
  import BaseDrawer from '@/components/shared/BaseCard.vue';
  import { useSubscriptionPlans } from '@/composables/query/subscriptions/useSubscriptions';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import { formatCurrency, formatDate } from '@/utils/uiHelpers';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto }>();
  const { data: availablePlans } = useSubscriptionPlans();

  const activePlanDef = computed(
    () => availablePlans.value?.find((p) => p.id === props.subscription.planId) || null,
  );
  const isTrial = computed(() => (props.subscription.planId || '').toUpperCase().includes('TRIAL'));

  const paidUsers = computed(() => props.subscription.purchasedUsers || 0);
  const bonusUsers = computed(() => props.subscription.bonusUsers || 0);
  const paidBranches = computed(() => props.subscription.purchasedBranches || 0);
  const bonusBranches = computed(() => props.subscription.bonusBranches || 0);

  const basePrice = computed(() => (isTrial.value ? 0 : activePlanDef.value?.baseFee || 0));
  const userCost = computed(() =>
    isTrial.value ? 0 : paidUsers.value * (activePlanDef.value?.userPrice || 0),
  );
  const branchCost = computed(() =>
    isTrial.value ? 0 : paidBranches.value * (activePlanDef.value?.branchPrice || 0),
  );

  const currency = computed(() => props.subscription.currency || 'USD');
</script>

<template>
  <BaseDrawer
    no-padding
    class="bg-surface-0 dark:bg-surface-800 rounded-xl border border-surface-200 dark:border-surface-700 shadow-sm flex flex-col h-full relative overflow-hidden"
  >
    <div class="p-4 pb-0 flex justify-between items-start z-10 relative">
      <div>
        <div class="flex items-center gap-1.5 mb-0.5">
          <span class="text-[10px] font-bold text-surface-500 uppercase tracking-wider">
            Current Plan
          </span>
          <Tag
            :value="subscription.planId"
            severity="secondary"
            class="text-[9px] py-0 px-1.5 h-4 font-mono"
          />
        </div>
        <h2
          class="text-lg font-bold text-surface-900 dark:text-surface-0 flex items-center gap-2 leading-tight"
        >
          {{ activePlanDef?.name || subscription.planId }}
        </h2>
      </div>

      <div v-if="subscription.cancelAtPeriodEnd" class="flex flex-col items-end">
        <Tag value="CANCELLING" severity="warn" class="text-[10px] font-bold mb-1" />
        <span class="text-[9px] text-orange-600 font-bold">
          Ends {{ formatDate(subscription.endDate) }}
        </span>
      </div>
      <Tag
        v-else
        :value="isTrial ? 'TRIAL' : 'ACTIVE'"
        :severity="isTrial ? 'info' : 'success'"
        class="text-[10px] font-bold"
      />
    </div>

    <div class="p-4 flex-grow flex flex-col justify-center z-10 relative">
      <div
        class="bg-surface-50 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-lg p-3 text-xs space-y-2"
      >
        <div class="flex justify-between items-center text-surface-700 dark:text-surface-300">
          <div class="flex items-center gap-1.5">
            <div
              class="w-5 h-5 rounded bg-surface-200 dark:bg-surface-800 flex items-center justify-center text-surface-500"
            >
              <i class="pi pi-box text-[10px]"></i>
            </div>
            <span>Base Subscription</span>
          </div>
          <span class="font-medium font-mono">{{ formatCurrency(basePrice, currency) }}</span>
        </div>

        <div
          class="flex justify-between items-center text-surface-700 dark:text-surface-300"
          :class="{ 'opacity-50': paidUsers === 0 && bonusUsers === 0 }"
        >
          <div class="flex items-center gap-1.5">
            <div
              class="w-5 h-5 rounded bg-purple-100 dark:bg-purple-500/20 flex items-center justify-center text-purple-600 dark:text-purple-400"
            >
              <i class="pi pi-users text-[10px]"></i>
            </div>
            <div class="flex items-baseline gap-1">
              <span>Add-on Users</span>
              <span v-if="paidUsers > 0 || bonusUsers > 0" class="text-[9px] text-surface-500">
                ({{ paidUsers }} Paid / {{ bonusUsers }} Free)
              </span>
            </div>
          </div>
          <span class="font-medium font-mono">{{ formatCurrency(userCost, currency) }}</span>
        </div>

        <div
          class="flex justify-between items-center text-surface-700 dark:text-surface-300"
          :class="{ 'opacity-50': paidBranches === 0 && bonusBranches === 0 }"
        >
          <div class="flex items-center gap-1.5">
            <div
              class="w-5 h-5 rounded bg-orange-100 dark:bg-orange-500/20 flex items-center justify-center text-orange-600 dark:text-orange-400"
            >
              <i class="pi pi-building text-[10px]"></i>
            </div>
            <div class="flex items-baseline gap-1">
              <span>Add-on Branches</span>
              <span
                v-if="paidBranches > 0 || bonusBranches > 0"
                class="text-[9px] text-surface-500"
              >
                ({{ paidBranches }} Paid / {{ bonusBranches }} Free)
              </span>
            </div>
          </div>
          <span class="font-medium font-mono">{{ formatCurrency(branchCost, currency) }}</span>
        </div>

        <div class="h-px bg-surface-200 dark:bg-surface-700 my-1"></div>

        <div class="flex justify-between items-center">
          <span class="font-bold text-surface-900 dark:text-surface-0">Total Fee</span>
          <span class="text-base font-bold text-primary-600 dark:text-primary-400">
            {{ formatCurrency(subscription.fee, currency) }}
          </span>
        </div>
      </div>
    </div>

    <div
      class="px-4 py-2 bg-surface-50 dark:bg-surface-900 border-t border-surface-200 dark:border-surface-700 flex justify-between items-center text-[10px] text-surface-500 relative z-10"
    >
      <div class="flex flex-col">
        <span class="uppercase tracking-wider font-bold text-[9px] mb-0.5">Start</span>
        <span class="font-medium text-surface-700 dark:text-surface-300">
          {{ formatDate(subscription.startDate) }}
        </span>
      </div>
      <div class="flex flex-col items-center">
        <span
          v-if="subscription.autoRenew && !subscription.cancelAtPeriodEnd"
          class="text-green-600 dark:text-green-400 font-bold flex items-center gap-1"
        >
          <i class="pi pi-sync text-[9px]"></i>
          Auto-Renew
        </span>
        <span
          v-else-if="subscription.cancelAtPeriodEnd"
          class="text-red-500 font-bold flex items-center gap-1"
        >
          <i class="pi pi-ban text-[9px]"></i>
          Canceling
        </span>
        <span v-else class="text-orange-600 dark:text-orange-400 font-bold flex items-center gap-1">
          <i class="pi pi-info-circle text-[9px]"></i>
          Manual
        </span>
      </div>
      <div class="flex flex-col text-right">
        <span class="uppercase tracking-wider font-bold text-[9px] mb-0.5">Expires</span>
        <span class="font-medium text-surface-700 dark:text-surface-300">
          {{ formatDate(subscription.endDate) }}
        </span>
      </div>
    </div>

    <div
      class="absolute top-0 right-0 w-24 h-24 bg-primary-50 dark:bg-primary-900/10 rounded-bl-full -mr-8 -mt-8 pointer-events-none z-0"
    ></div>
  </BaseDrawer>
</template>
