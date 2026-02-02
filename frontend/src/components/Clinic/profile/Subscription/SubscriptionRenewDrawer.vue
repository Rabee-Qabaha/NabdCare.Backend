<script setup lang="ts">
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import { SubscriptionType } from '@/types/backend';
  import { formatCurrency, formatDate } from '@/utils/uiHelpers';
  import Button from 'primevue/button';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  const props = defineProps<{
    visible: boolean;
    subscription: SubscriptionResponseDto;
  }>();

  const emit = defineEmits(['update:visible', 'success']);

  const { renewMutation } = useSubscriptionActions();

  const newEndDate = computed(() => {
    if (!props.subscription.endDate) return new Date();
    const d = new Date(props.subscription.endDate);
    if (props.subscription.type === SubscriptionType.Yearly) {
      d.setFullYear(d.getFullYear() + 1);
    } else {
      d.setMonth(d.getMonth() + 1);
    }
    return d;
  });

  const renewalCost = computed(() => props.subscription.fee || 0);

  const handleRenew = () => {
    renewMutation.mutate(
      { id: props.subscription.id, type: props.subscription.type },
      {
        onSuccess: () => {
          emit('success');
          emit('update:visible', false);
        },
      },
    );
  };
</script>

<template>
  <BaseDrawer
    :visible="visible"
    @update:visible="(v) => emit('update:visible', v)"
    title="Renew Subscription"
    subtitle="Extend your clinic's access for another cycle."
    icon="pi pi-refresh"
    width="md:!w-[480px]"
  >
    <div class="space-y-6">
      <!-- Main Confirmation Card -->
      <div
        class="bg-green-50 dark:bg-green-900/10 rounded-xl p-6 border border-green-100 dark:border-green-800/30 text-center"
      >
        <div
          class="w-16 h-16 bg-white dark:bg-green-800/20 rounded-full flex items-center justify-center mx-auto mb-4 shadow-sm"
        >
          <i class="pi pi-calendar-plus text-2xl text-green-600 dark:text-green-400"></i>
        </div>

        <h3 class="font-bold text-lg text-green-900 dark:text-green-100 mb-2">Confirm Renewal</h3>
        <p class="text-sm text-green-700 dark:text-green-300 leading-relaxed px-4">
          You are about to extend your
          <strong>{{ subscription.type }}</strong>
          subscription. This will add one full cycle to your current expiry date.
        </p>
      </div>

      <!-- Details List -->
      <div
        class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-800 rounded-xl overflow-hidden"
      >
        <!-- Current Expiry -->
        <div
          class="flex justify-between items-center p-4 border-b border-surface-100 dark:border-surface-800"
        >
          <div class="flex items-center gap-3">
            <i class="pi pi-clock text-surface-400"></i>
            <span class="text-sm font-medium text-surface-600 dark:text-surface-400">
              Current Expiry
            </span>
          </div>
          <span class="text-sm font-bold text-surface-900 dark:text-surface-0">
            {{ formatDate(subscription.endDate) }}
          </span>
        </div>

        <!-- New Expiry -->
        <div
          class="flex justify-between items-center p-4 border-b border-surface-100 dark:border-surface-800 bg-surface-50/50 dark:bg-surface-800/20"
        >
          <div class="flex items-center gap-3">
            <i class="pi pi-arrow-right text-green-500"></i>
            <span class="text-sm font-medium text-green-700 dark:text-green-400">
              New Expiry Date
            </span>
          </div>
          <Tag :value="formatDate(newEndDate)" severity="success" class="!font-bold" />
        </div>

        <!-- Cost -->
        <div class="flex justify-between items-center p-4">
          <div class="flex items-center gap-3">
            <i class="pi pi-wallet text-surface-400"></i>
            <span class="text-sm font-medium text-surface-600 dark:text-surface-400">
              Renewal Cost
            </span>
          </div>
          <span class="text-lg font-bold text-surface-900 dark:text-surface-0">
            {{ formatCurrency(renewalCost, subscription.currency) }}
          </span>
        </div>
      </div>

      <div
        class="p-4 bg-blue-50 dark:bg-blue-900/10 rounded-xl border border-blue-100 dark:border-blue-800/30 flex gap-3"
      >
        <i class="pi pi-info-circle text-blue-500 mt-0.5"></i>
        <p class="text-xs text-blue-700 dark:text-blue-300 leading-relaxed">
          The renewal fee will be added to your next invoice. If you have auto-renew enabled, this
          happens automatically on {{ formatDate(subscription.endDate) }}.
        </p>
      </div>
    </div>

    <template #footer="{ close }">
      <div class="w-full flex gap-3">
        <Button
          label="Cancel"
          severity="secondary"
          variant="outlined"
          class="!w-[30%]"
          @click="close"
        />
        <Button
          label="Confirm Renewal"
          icon="pi pi-check"
          class="flex-1 !bg-green-600 !border-green-600 hover:!bg-green-700"
          :loading="renewMutation.isPending.value"
          @click="handleRenew"
        />
      </div>
    </template>
  </BaseDrawer>
</template>
