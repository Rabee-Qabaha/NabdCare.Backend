<template>
  <div class="card border-1 surface-border mb-0">
    <div class="mb-4 flex justify-between">
      <div>
        <span class="mb-4 block font-medium text-muted-color">{{ label }}</span>
        <div class="text-3xl font-bold text-surface-900 dark:text-surface-0">
          <Skeleton v-if="loading" width="3rem" height="2rem" />
          <span v-else>{{ formattedValue }}</span>
        </div>
        <div v-if="subtitle" class="mt-1 text-xs text-muted-color">
          {{ subtitle }}
        </div>
      </div>
      <div
        v-if="icon"
        :class="['flex items-center justify-center rounded-border', colorClasses.bg]"
        style="width: 2.5rem; height: 2.5rem"
      >
        <i :class="[icon, colorClasses.text, '!text-xl']"></i>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue';

  const props = withDefaults(
    defineProps<{
      label: string;
      value: number | string;
      icon?: string;
      color?: 'blue' | 'green' | 'orange' | 'cyan' | 'purple';
      subtitle?: string;
      loading?: boolean;
      format?: 'number' | 'currency' | 'percentage';
    }>(),
    {
      color: 'blue',
      loading: false,
      format: 'number',
    },
  );

  const colorMap = {
    blue: { bg: 'bg-blue-100 dark:bg-blue-400/10', text: 'text-blue-500' },
    green: { bg: 'bg-green-100 dark:bg-green-400/10', text: 'text-green-500' },
    orange: {
      bg: 'bg-orange-100 dark:bg-orange-400/10',
      text: 'text-orange-500',
    },
    cyan: { bg: 'bg-cyan-100 dark:bg-cyan-400/10', text: 'text-cyan-500' },
    purple: {
      bg: 'bg-purple-100 dark:bg-purple-400/10',
      text: 'text-purple-500',
    },
  };

  const colorClasses = computed(() => colorMap[props.color ?? 'blue']);

  const formattedValue = computed(() => {
    if (typeof props.value === 'string') return props.value;
    switch (props.format) {
      case 'currency':
        return new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
        }).format(props.value);
      case 'percentage':
        return `${props.value}%`;
      default:
        return props.value.toLocaleString();
    }
  });
</script>
