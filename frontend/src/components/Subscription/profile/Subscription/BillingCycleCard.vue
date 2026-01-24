<script setup lang="ts">
  import { useSubscriptionActions } from '@/composables/query/subscriptions/useSubscriptionActions';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import ToggleSwitch from 'primevue/toggleswitch';
  import { computed } from 'vue';

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
    // Prevent division by zero
    return Math.max(1, Math.ceil(diff / (1000 * 3600 * 24)));
  });

  const daysElapsed = computed(() => {
    const diff = now.getTime() - start.value.getTime();
    const days = Math.floor(diff / (1000 * 3600 * 24));
    // Clamp between 0 and totalDays
    return Math.min(totalDays.value, Math.max(0, days));
  });

  const daysRemaining = computed(() => Math.max(0, totalDays.value - daysElapsed.value));

  const progressPercent = computed(() => {
    return (daysElapsed.value / totalDays.value) * 100;
  });

  const handleToggle = (val: boolean) => {
    emit('update:autoRenew', val);
  };
</script>

<template>
  <div
    class="bg-surface-0 dark:bg-[#27272a] rounded-xl p-6 border border-transparent dark:border-surface-700 shadow dark:shadow-sm transition-colors duration-300"
  >
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

      <div
        class="relative h-3 bg-surface-100 dark:bg-surface-950 rounded-full w-full overflow-hidden"
      >
        <div
          class="absolute top-0 left-0 h-full bg-primary-500 rounded-full transition-all duration-500"
          :style="{ width: `${progressPercent}%` }"
        ></div>
      </div>

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
  </div>
</template>
