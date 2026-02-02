<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import ProgressBar from 'primevue/progressbar';
  import ToggleSwitch from 'primevue/toggleswitch';
  import { computed, onMounted, ref } from 'vue';

  const props = defineProps<{ subscription: SubscriptionResponseDto; clinicId?: string }>();

  // Emits for the toggle action
  const emit = defineEmits<{
    (e: 'update:autoRenew', value: boolean): void;
  }>();

  const { toggleAutoRenewMutation } = useSubscriptionActions();

  // --- Computed Logic ---

  const start = computed(() => new Date(props.subscription.startDate));
  const end = computed(() => new Date(props.subscription.endDate));
  const now = new Date();

  const formatDate = (date: Date) => {
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const totalDays = computed(() => {
    const diff = end.value.getTime() - start.value.getTime();
    return Math.max(1, Math.ceil(diff / (1000 * 3600 * 24)));
  });

  const daysElapsed = computed(() => {
    const diff = now.getTime() - start.value.getTime();
    const days = Math.floor(diff / (1000 * 3600 * 24));
    return Math.min(totalDays.value, Math.max(0, days));
  });

  const daysRemaining = computed(() => Math.max(0, totalDays.value - daysElapsed.value));

  // The actual calculated target
  const targetPercent = computed(() => {
    return (daysElapsed.value / totalDays.value) * 100;
  });

  // 1. Start at 0 to force the "empty" state initially
  const displayedPercent = ref(0);

  // 2. Animate to the target value after the component mounts
  onMounted(() => {
    setTimeout(() => {
      displayedPercent.value = targetPercent.value;
    }, 300); // 300ms delay ensures the UI is ready to animate
  });
</script>

<template>
  <BaseCard>
    <div class="flex items-center gap-3 mb-6">
      <div
        class="w-8 h-8 rounded-lg bg-primary-50 dark:bg-primary-900/20 text-primary-600 flex items-center justify-center"
      >
        <i class="pi pi-sync"></i>
      </div>
      <h3 class="font-bold text-lg text-surface-900 dark:text-surface-0">
        Billing Cycle & Timeline
      </h3>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 mb-8">
      <div>
        <div
          class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider mb-1"
        >
          Start Date
        </div>
        <div class="font-bold text-surface-900 dark:text-surface-0">
          {{ formatDate(start) }}
        </div>
      </div>

      <div>
        <div
          class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider mb-1"
        >
          Next Billing Date
        </div>
        <div class="font-bold text-primary-600 dark:text-primary-400">
          {{ formatDate(end) }}
        </div>
      </div>

      <div>
        <div
          class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider mb-1"
        >
          Auto-Renew
        </div>
        <div class="flex items-center gap-2">
          <span class="font-bold text-surface-900 dark:text-surface-0">
            {{ props.subscription.autoRenew ? 'Enabled' : 'Disabled' }}
          </span>
          <ToggleSwitch
            :model-value="props.subscription.autoRenew"
            :disabled="toggleAutoRenewMutation.isPending.value"
            @update:model-value="
              (v) => toggleAutoRenewMutation.mutate({ id: props.subscription.id, enable: v })
            "
          />
        </div>
      </div>
    </div>

    <div>
      <div class="flex justify-between mb-2 text-sm font-medium">
        <span class="text-surface-500 dark:text-surface-400">Cycle Progress</span>
        <span class="text-surface-900 dark:text-surface-0 font-bold">
          {{ daysElapsed }} / {{ totalDays }} Days Elapsed
        </span>
      </div>

      <ProgressBar
        :value="displayedPercent"
        :showValue="false"
        class="!h-3 !bg-surface-100 dark:!bg-surface-950 !rounded-full"
        :pt="{
          value: {
            class: '!bg-primary-500 !rounded-full !transition-all !duration-1000 !ease-out',
          },
        }"
      />

      <div
        class="flex justify-between mt-2 text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-wider"
      >
        <span>{{ formatDate(start) }}</span>
        <span class="text-primary-600 dark:text-primary-400">
          {{ daysRemaining }} Days Remaining
        </span>
        <span>{{ formatDate(end) }}</span>
      </div>
    </div>
  </BaseCard>
</template>
