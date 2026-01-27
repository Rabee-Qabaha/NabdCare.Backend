<script setup lang="ts">
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import { formatDate } from '@/utils/uiHelpers';
  import Button from 'primevue/button';
  import { computed } from 'vue';

  const props = defineProps<{
    subscription: SubscriptionResponseDto;
    visible: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'update:visible', value: boolean): void;
    (e: 'cancel'): void;
  }>();

  const { cancelMutation } = useSubscriptionActions();

  const endDate = computed(() => new Date(props.subscription.endDate));
  const now = new Date();

  // Calculate days remaining for the "Badge"
  const daysRemaining = computed(() => {
    const diff = endDate.value.getTime() - now.getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 3600 * 24)));
  });

  const handleCancel = () => {
    cancelMutation.mutate(props.subscription.id, {
      onSuccess: () => {
        emit('update:visible', false);
        emit('cancel');
      },
    });
  };
</script>

<template>
  <BaseDrawer
    :visible="visible"
    title="Cancel Subscription"
    subtitle="Manage your plan renewal settings"
    icon="pi pi-ban"
    width="md:!w-[450px]"
    @update:visible="(v) => emit('update:visible', v)"
  >
    <div class="space-y-8 pb-4">
      <div class="flex flex-col items-center text-center px-4 mt-4">
        <div
          class="w-16 h-16 rounded-2xl bg-red-50 dark:bg-red-900/20 flex items-center justify-center mb-4"
        >
          <i class="pi pi-exclamation-triangle text-3xl text-red-500"></i>
        </div>
        <h3 class="text-xl font-bold text-surface-900 dark:text-surface-0 mb-2">Are you sure?</h3>
        <p class="text-surface-500 dark:text-surface-400 text-sm leading-relaxed max-w-xs mx-auto">
          Your subscription will be set to cancel at the end of the current billing period.
        </p>
      </div>

      <div
        class="bg-surface-50 dark:bg-surface-900 border border-surface-200 dark:border-surface-800 rounded-xl p-5 relative overflow-hidden"
      >
        <div
          class="absolute left-[29px] top-8 bottom-8 w-0.5 bg-surface-200 dark:bg-surface-700"
        ></div>

        <div class="flex gap-4 relative z-10 mb-6">
          <div
            class="w-8 h-8 rounded-full bg-surface-0 dark:bg-surface-800 border-2 border-red-500 flex items-center justify-center shrink-0 shadow-sm"
          >
            <i class="pi pi-ban text-red-500 text-xs font-bold"></i>
          </div>
          <div>
            <div class="text-sm font-bold text-surface-900 dark:text-surface-0">Cancel Today</div>
            <div class="text-xs text-surface-500 mt-0.5">{{ formatDate(now) }}</div>
          </div>
        </div>

        <div class="flex gap-4 relative z-10 mb-6">
          <div class="w-8 flex justify-center shrink-0">
            <div class="w-1.5 h-1.5 rounded-full bg-surface-300 dark:bg-surface-600 mt-1.5"></div>
          </div>
          <div
            class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 rounded-lg p-3 text-xs text-surface-600 dark:text-surface-300 w-full shadow-sm"
          >
            <span class="font-bold text-surface-900 dark:text-surface-0">
              Full Access Continues
            </span>
            <br />
            You can still use all features for the next
            <strong class="text-primary-600 dark:text-primary-400">{{ daysRemaining }} days</strong>
            .
          </div>
        </div>

        <div class="flex gap-4 relative z-10">
          <div
            class="w-8 h-8 rounded-full bg-surface-200 dark:bg-surface-700 flex items-center justify-center shrink-0"
          >
            <i class="pi pi-calendar-times text-surface-500 text-xs"></i>
          </div>
          <div>
            <div class="text-sm font-bold text-surface-900 dark:text-surface-0">Plan Expires</div>
            <div class="text-xs text-surface-500 mt-0.5">{{ formatDate(endDate) }}</div>
          </div>
        </div>
      </div>

      <div class="text-center">
        <p class="text-xs text-surface-400 dark:text-surface-500">
          You can resume your plan anytime before the expiration date.
        </p>
      </div>
    </div>

    <template #footer>
      <div class="w-full flex gap-3 pt-2 pb-4">
        <Button
          label="Keep Plan"
          severity="secondary"
          variant="outlined"
          class="!h-11 !w-[30%] !font-bold !border-surface-300 dark:!border-surface-600"
          @click="emit('update:visible', false)"
        />
        <Button
          label="Confirm Cancel"
          severity="danger"
          icon="pi pi-ban"
          class="!h-11 flex-1 !font-bold"
          :loading="cancelMutation.isPending.value"
          @click="handleCancel"
        />
      </div>
    </template>
  </BaseDrawer>
</template>
