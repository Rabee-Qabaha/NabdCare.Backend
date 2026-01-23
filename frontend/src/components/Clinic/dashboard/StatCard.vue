<template>
  <div class="bg-surface-0 dark:bg-surface-800 p-4 rounded-xl border border-surface-200 dark:border-surface-700 shadow-sm flex flex-col justify-between h-full transition-all hover:shadow-md">
    <div class="flex justify-between items-start mb-2">
      <span class="text-surface-500 font-medium text-sm">{{ label }}</span>
      <div 
        class="w-8 h-8 rounded-lg flex items-center justify-center text-lg"
        :class="iconBgClass"
      >
        <i :class="icon" />
      </div>
    </div>
    
    <div class="flex items-baseline gap-2 mt-2">
      <span class="text-2xl font-bold text-surface-900 dark:text-surface-0">{{ value }}</span>
      <span v-if="subtext" class="text-xs text-surface-400">{{ subtext }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  label: string;
  value: number | string;
  icon: string;
  color?: 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'purple';
  subtext?: string;
}>();

const iconBgClass = computed(() => {
  const map: Record<string, string> = {
    primary: 'bg-primary-50 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400',
    success: 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-400',
    warning: 'bg-orange-50 text-orange-600 dark:bg-orange-500/20 dark:text-orange-400',
    danger:  'bg-red-50 text-red-600 dark:bg-red-500/20 dark:text-red-400',
    info:    'bg-blue-50 text-blue-600 dark:bg-blue-500/20 dark:text-blue-400',
    purple:  'bg-purple-50 text-purple-600 dark:bg-purple-500/20 dark:text-purple-400'
  };
  return map[props.color || 'primary'];
});
</script>