<template>
  <BaseCard class="bg-surface-50/50 dark:bg-surface-800/20 flex-1 flex flex-col justify-center">
    <div class="flex items-center gap-4">
      <!-- Avatar & Identity -->
      <Avatar
        :label="(primaryAdminName || 'A')[0]"
        shape="circle"
        class="!w-10 !h-10 bg-surface-200 text-surface-600 dark:bg-surface-700 dark:text-surface-300 shrink-0"
      />

      <div class="flex-1 min-w-0">
        <div class="text-[10px] uppercase font-bold text-surface-500 mb-0.5">
          Primary Administrator
        </div>
        <div class="text-sm font-bold text-surface-900 dark:text-surface-0 truncate">
          {{ primaryAdminName || 'System' }}
        </div>
      </div>

      <!-- Audit Meta (Right Aligned) -->
      <div class="flex gap-6 text-right px-4 border-l border-surface-200 dark:border-surface-700">
        <div>
          <div class="text-[9px] uppercase font-bold text-surface-400 mb-0.5">Created</div>
          <div class="text-[10px] font-medium text-surface-600 dark:text-surface-300">
            {{ formatDate(createdAt).split(',')[0] }}
          </div>
        </div>
        <div>
          <div class="text-[9px] uppercase font-bold text-surface-400 mb-0.5">Last Active</div>
          <div class="text-[10px] font-medium text-surface-600 dark:text-surface-300">
            {{ lastLoginAt ? formatDate(lastLoginAt).split(',')[0] : 'Never' }}
          </div>
        </div>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import Avatar from 'primevue/avatar';

  defineProps<{
    primaryAdminName?: string;
    createdAt?: Date | string;
    lastLoginAt?: Date | string;
  }>();

  const formatDate = (date?: string | Date) => {
    if (!date) return 'N/A';
    // Handle the "0001-01-01" case
    if (new Date(date).getFullYear() < 2000) return 'Never';

    return new Date(date).toLocaleString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };
</script>
