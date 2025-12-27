// src/components/Subscription/SubscriptionActions.vue
<script setup lang="ts">
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import { SubscriptionStatus, SubscriptionType } from '@/types/backend';
  import { formatDate } from '@/utils/uiHelpers';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import ToggleSwitch from 'primevue/toggleswitch';
  import { computed, ref } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto }>();
  const emit = defineEmits(['edit', 'renew']);

  const {
    renewMutation,
    toggleAutoRenewMutation,
    cancelMutation,
    updateMutation,
    canPurchaseAddons,
    canRenew,
    canToggleAutoRenew,
    canCancel,
  } = useSubscriptionActions();

  const showRenewDialog = ref(false);
  const showTerminateDialog = ref(false);

  const currentEndDate = computed(() => new Date(props.subscription.endDate));
  const newEndDate = computed(() => {
    const d = new Date(currentEndDate.value);
    props.subscription.type === SubscriptionType.Yearly
      ? d.setFullYear(d.getFullYear() + 1)
      : d.setMonth(d.getMonth() + 1);
    return d;
  });

  const handleRenew = () =>
    renewMutation.mutate(
      { id: props.subscription.id, type: props.subscription.type },
      {
        onSuccess: () => {
          showRenewDialog.value = false;
          emit('renew');
        },
      },
    );

  const handleTerminate = () =>
    cancelMutation.mutate(props.subscription.id, {
      onSuccess: () => (showTerminateDialog.value = false),
    });

  const handleResume = () => {
    // To resume, we set cancelAtPeriodEnd = false and autoRenew = true
    updateMutation.mutate({
      id: props.subscription.id,
      data: {
        cancelAtPeriodEnd: false,
        autoRenew: true,
        extraBranches: props.subscription.purchasedBranches,
        extraUsers: props.subscription.purchasedUsers,
        bonusBranches: 0,
        bonusUsers: 0,
        gracePeriodDays: 0,
        status: SubscriptionStatus.Active,
        endDate: undefined,
      },
    });
  };
</script>

<template>
  <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
    <div
      v-if="canPurchaseAddons && !subscription.cancelAtPeriodEnd"
      class="action-card group"
      @click="emit('edit')"
    >
      <div
        class="icon-wrapper bg-purple-50 dark:bg-purple-900/20 text-purple-600 dark:text-purple-400"
      >
        <i class="pi pi-sliders-h text-lg"></i>
      </div>
      <span class="font-bold text-surface-900 dark:text-surface-0 text-sm">Manage Add-ons</span>
      <span class="text-xs text-surface-500 mt-1 leading-tight">Users & Branches</span>
    </div>

    <div
      v-if="canRenew && !subscription.cancelAtPeriodEnd"
      class="action-card group hover:border-green-500"
      @click="showRenewDialog = true"
    >
      <div class="icon-wrapper bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400">
        <i v-if="renewMutation.isPending.value" class="pi pi-spin pi-spinner text-lg"></i>
        <i v-else class="pi pi-refresh text-lg"></i>
      </div>
      <span class="font-bold text-surface-900 dark:text-surface-0 text-sm">Renew Plan</span>
      <span class="text-xs text-surface-500 mt-1 leading-tight">Extend +1 Cycle</span>
    </div>

    <div
      v-if="canToggleAutoRenew && !subscription.cancelAtPeriodEnd"
      class="action-card cursor-default hover:border-blue-500"
    >
      <div class="icon-wrapper bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400">
        <i class="pi pi-sync text-lg"></i>
      </div>
      <div class="flex flex-col items-center gap-1">
        <span class="font-bold text-surface-900 dark:text-surface-0 text-sm">Auto-Renew</span>
        <div class="scale-90 h-6 flex items-center" @click.stop>
          <ToggleSwitch
            :modelValue="props.subscription.autoRenew"
            @update:modelValue="
              (v) => toggleAutoRenewMutation.mutate({ id: props.subscription.id, enable: v })
            "
            :disabled="toggleAutoRenewMutation.isPending.value"
          />
        </div>
      </div>
    </div>

    <div
      v-if="subscription.cancelAtPeriodEnd"
      class="action-card group border-green-200 hover:border-green-500 bg-green-50/50"
      @click="handleResume"
    >
      <div class="icon-wrapper bg-green-100 text-green-700">
        <i v-if="updateMutation.isPending.value" class="pi pi-spin pi-spinner text-lg"></i>
        <i v-else class="pi pi-undo text-lg"></i>
      </div>
      <span class="font-bold text-green-800 text-sm">Resume Plan</span>
      <span class="text-xs text-green-600 mt-1 leading-tight">Cancel Termination</span>
    </div>

    <div
      v-else-if="canCancel"
      class="action-card group hover:border-red-500"
      @click="showTerminateDialog = true"
    >
      <div class="icon-wrapper bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400">
        <i v-if="cancelMutation.isPending.value" class="pi pi-spin pi-spinner text-lg"></i>
        <i v-else class="pi pi-ban text-lg"></i>
      </div>
      <span
        class="font-bold text-surface-900 dark:text-surface-0 text-sm text-red-600 dark:text-red-400"
      >
        Cancel Plan
      </span>
      <span class="text-xs text-surface-500 mt-1 leading-tight">Stop at Period End</span>
    </div>

    <div
      v-if="!canPurchaseAddons && !canRenew && !canToggleAutoRenew && !canCancel"
      class="col-span-full text-center p-3 text-surface-500 text-sm border rounded-xl border-dashed"
    >
      View Only Access
    </div>
  </div>

  <Dialog
    v-model:visible="showRenewDialog"
    modal
    header="Renew Subscription"
    :style="{ width: '350px' }"
  >
    <div class="flex flex-col gap-3">
      <div class="bg-green-50 dark:bg-green-900/20 p-3 rounded-lg flex gap-3">
        <i class="pi pi-calendar-plus text-green-600 text-xl"></i>
        <div>
          <h4 class="font-bold text-green-900 dark:text-green-100 text-sm m-0">Confirm Renewal</h4>
          <p class="text-xs text-green-700 dark:text-green-300 mt-1">
            Queues a new
            <strong>{{ props.subscription.type }}</strong>
            cycle.
          </p>
        </div>
      </div>
      <div class="flex justify-between items-center text-sm border-t pt-3 border-surface-200">
        <span class="text-surface-500">New Expiry</span>
        <span class="font-bold text-green-600">{{ formatDate(newEndDate) }}</span>
      </div>
    </div>
    <template #footer>
      <Button
        label="Cancel"
        text
        size="small"
        severity="secondary"
        @click="showRenewDialog = false"
      />
      <Button
        label="Renew"
        icon="pi pi-check"
        size="small"
        severity="success"
        :loading="renewMutation.isPending.value"
        @click="handleRenew"
      />
    </template>
  </Dialog>

  <Dialog
    v-model:visible="showTerminateDialog"
    modal
    header="Cancel Subscription"
    :style="{ width: '350px' }"
  >
    <div class="bg-orange-50 dark:bg-orange-900/20 p-3 rounded-lg flex gap-3 mb-2">
      <i class="pi pi-info-circle text-orange-600 text-xl"></i>
      <div>
        <h4 class="font-bold text-orange-900 dark:text-orange-100 text-sm m-0">
          Graceful Cancellation
        </h4>
        <p class="text-xs text-orange-700 dark:text-orange-300 mt-1">
          Access continues until
          <strong>{{ formatDate(subscription.endDate) }}</strong>
          . No further charges will be made.
        </p>
      </div>
    </div>
    <template #footer>
      <Button
        label="Keep Plan"
        text
        size="small"
        severity="secondary"
        @click="showTerminateDialog = false"
      />
      <Button
        label="Confirm Cancel"
        severity="danger"
        size="small"
        icon="pi pi-ban"
        :loading="cancelMutation.isPending.value"
        @click="handleTerminate"
      />
    </template>
  </Dialog>
</template>

<style scoped>
  .action-card {
    @apply flex h-28 cursor-pointer flex-col items-center justify-center rounded-xl border border-surface-200 bg-surface-0 p-3 text-center shadow-sm transition-all hover:shadow-md dark:border-surface-700 dark:bg-surface-800;
  }
  .icon-wrapper {
    @apply mb-2 flex h-10 w-10 items-center justify-center rounded-full transition-transform group-hover:scale-110;
  }
</style>
