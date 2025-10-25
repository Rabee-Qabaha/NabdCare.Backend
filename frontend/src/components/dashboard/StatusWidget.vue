// src/components/dashboard/StatusWidget.vue
<template>
  <div class="card mb-0">
    <div class="flex justify-between mb-4">
      <div>
        <span class="block text-muted-color font-medium mb-4">{{
          header
        }}</span>
        <div class="text-surface-900 dark:text-surface-0 font-medium text-xl">
          {{ value }}
        </div>
      </div>

      <!-- Icon container -->
      <div
        :class="[
          'flex items-center justify-center rounded-border',
          colorClasses.bg,
        ]"
        style="width: 2.5rem; height: 2.5rem"
      >
        <i :class="[icon, colorClasses.text, '!text-xl']"></i>
      </div>
    </div>

    <span class="text-primary font-medium">{{ static }}</span>
    <span class="mx-1"></span>
    <span class="text-muted-color">{{ staticText }}</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";

const props = defineProps<{
  header?: string;
  value?: number | string;
  icon?: string;
  color?: "blue" | "green" | "orange" | "cyan" | "purple"; // restrict to known colors
  static?: string;
  staticText?: string;
}>();

// ✅ Map color → classes (safe for Tailwind purge)
const colorMap = {
  blue: { bg: "bg-blue-100 dark:bg-blue-400/10", text: "text-blue-500" },
  green: { bg: "bg-green-100 dark:bg-green-400/10", text: "text-green-500" },
  orange: {
    bg: "bg-orange-100 dark:bg-orange-400/10",
    text: "text-orange-500",
  },
  cyan: { bg: "bg-cyan-100 dark:bg-cyan-400/10", text: "text-cyan-500" },
  purple: {
    bg: "bg-purple-100 dark:bg-purple-400/10",
    text: "text-purple-500",
  },
};

const colorClasses = computed(() => colorMap[props.color ?? "blue"]);
</script>
