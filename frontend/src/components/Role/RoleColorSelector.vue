<template>
  <div class="flex flex-col gap-2">
    <div class="flex items-center justify-between">
      <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
        {{ label }}
        <span v-if="loading" class="text-[10px] font-normal text-surface-400 ml-2">
          (Loading recommended...)
        </span>
      </label>
      <div v-if="modelValue" class="text-xs text-surface-500 font-mono">
        {{ modelValue }}
      </div>
    </div>

    <div class="flex items-center gap-2 overflow-x-auto pb-2 custom-scrollbar flex-nowrap">
      <!-- Custom Color Picker Trigger -->
      <div
        class="relative w-8 h-8 rounded-full border border-dashed border-surface-300 dark:border-surface-600 flex items-center justify-center text-surface-400 hover:bg-surface-50 dark:hover:bg-surface-800 cursor-pointer transition-colors shrink-0 group"
        title="Custom Color"
      >
        <input
          :value="modelValue"
          type="color"
          class="absolute inset-0 opacity-0 cursor-pointer w-full h-full"
          @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
        />
        <i
          class="pi pi-palette text-xs group-hover:text-primary-500 transition-colors"
          :class="{ 'text-primary-500': !isPresetColor }"
        ></i>
      </div>

      <div class="w-px h-6 bg-surface-200 dark:bg-surface-700 mx-1 shrink-0"></div>

      <!-- Preset Colors -->
      <button
        v-for="color in colors"
        :key="color"
        type="button"
        class="w-8 h-8 rounded-full border border-surface-200 dark:border-surface-700 shadow-sm flex items-center justify-center cursor-pointer transition-transform hover:scale-110 shrink-0 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-surface-300 dark:focus:ring-surface-600"
        :style="{ backgroundColor: color }"
        @click="$emit('update:modelValue', color)"
      >
        <i
          v-if="modelValue === color"
          class="pi pi-check text-xs text-white font-bold shadow-sm"
        ></i>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { computed } from 'vue';

  interface Props {
    modelValue: string | null;
    colors?: string[];
    label?: string;
    loading?: boolean;
  }

  const props = withDefaults(defineProps<Props>(), {
    modelValue: '#3B82F6',
    colors: () => [],
    label: 'Role Color',
    loading: false,
  });

  defineEmits<{
    (e: 'update:modelValue', value: string): void;
  }>();

  const isPresetColor = computed(() => {
    return props.colors.includes(props.modelValue || '');
  });
</script>

<style scoped>
  .custom-scrollbar::-webkit-scrollbar {
    height: 6px;
  }
  .custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: var(--surface-200);
    border-radius: 20px;
  }
  .dark .custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: var(--surface-700);
  }
</style>
