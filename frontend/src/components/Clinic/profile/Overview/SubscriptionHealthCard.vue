<template>
  <BaseCard
    class="bg-gradient-to-br from-surface-0 to-surface-50 dark:from-surface-800 dark:to-surface-900"
  >
    <div class="flex justify-between items-center mb-4">
      <div class="flex items-center gap-2">
        <i class="pi pi-verified text-primary-500"></i>
        <h3 class="font-bold text-surface-900 dark:text-surface-0">Subscription Health</h3>
      </div>
      <Tag
        :value="subscriptionStatus"
        :severity="subscriptionStatus === 'Active' ? 'success' : 'warn'"
        class="rounded-full px-3"
      />
    </div>

    <div class="grid grid-cols-2 gap-4">
      <div
        class="p-3 bg-white dark:bg-surface-800 rounded-lg border border-surface-100 dark:border-surface-700 shadow-sm"
      >
        <div class="text-[10px] text-surface-400 uppercase font-bold mb-1">Current Plan</div>
        <div class="text-lg font-bold text-surface-900 dark:text-surface-0">
          {{ subscriptionPlan || 'No Plan' }}
        </div>
      </div>
      <div
        class="p-3 bg-white dark:bg-surface-800 rounded-lg border border-surface-100 dark:border-surface-700 shadow-sm"
      >
        <div class="text-[10px] text-surface-400 uppercase font-bold mb-1">Expires On</div>
        <div
          class="text-sm font-bold text-surface-900 dark:text-surface-0 flex items-center gap-2 h-full"
        >
          <i class="pi pi-calendar text-surface-400"></i>
          {{ formatDate(subscriptionExpiresAt) }}
        </div>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import Tag from 'primevue/tag';

  defineProps<{
    subscriptionStatus: string;
    subscriptionPlan: string;
    subscriptionExpiresAt?: Date | string;
  }>();

  const formatDate = (date?: string | Date) => {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };
</script>
