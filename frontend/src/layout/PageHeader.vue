<!-- src/components/Layout/PageHeader.vue -->
<template>
  <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
    <!-- Left: Title + Description -->
    <div>
      <h1 class="font-bold text-3xl text-surface-900 dark:text-surface-0">
        {{ title }}
      </h1>

      <p v-if="description" class="text-surface-600 dark:text-surface-300 mt-1">
        {{ description }}
      </p>
    </div>

    <!-- Right: Stats -->
    <div
      v-if="stats?.length"
      class="flex flex-wrap items-center gap-6 text-surface-700 dark:text-surface-300"
    >
      <div v-for="(stat, i) in stats" :key="i" class="flex items-center gap-2">
        <i :class="stat.icon + ' text-base! leading-normal!'" />
        <span>{{ stat.label }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  defineProps<{
    title: string;
    description?: string;
    stats?: Array<{
      icon: string;
      label: string;
    }>;
  }>();
</script>
