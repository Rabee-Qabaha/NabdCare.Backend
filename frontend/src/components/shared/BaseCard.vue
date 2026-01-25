<template>
  <div
    class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-lg shadow-sm flex flex-col h-full hover:shadow-md transition-all"
    :class="{ 'p-5': !noPadding }"
  >
    <slot />
  </div>
</template>

<script setup lang="ts">
  defineProps({
    noPadding: {
      type: Boolean,
      default: false,
    },
  });
</script>
