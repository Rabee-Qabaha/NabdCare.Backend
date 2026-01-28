<template>
  <div class="flex flex-col gap-3">
    <div class="flex items-center justify-between">
      <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
        {{ label }}
      </label>
      <small v-if="hasHiddenIcons" class="text-[10px] text-surface-400">
        {{ hiddenMessage }}
      </small>
    </div>

    <!-- Search Input -->
    <span class="relative w-full">
      <i
        class="pi pi-search absolute top-2/4 -mt-2 right-3 text-surface-400 dark:text-surface-500"
      ></i>
      <InputText
        v-model="searchQuery"
        placeholder="Search icons..."
        class="w-full pr-10 !py-2 text-sm"
        :disabled="disabled"
      />
    </span>

    <!-- Icon Grid -->
    <div
      class="grid grid-cols-6 sm:grid-cols-8 gap-2 max-h-[200px] overflow-y-auto custom-scrollbar p-1 border border-surface-200 dark:border-surface-700 rounded-lg bg-surface-50 dark:bg-surface-900/50"
      :class="{ 'opacity-60 pointer-events-none': disabled }"
    >
      <button
        v-for="option in filteredOptions"
        :key="option.class"
        type="button"
        class="aspect-square flex flex-col items-center justify-center gap-1 rounded-md transition-all duration-200 hover:bg-surface-200 dark:hover:bg-surface-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
        :class="[
          modelValue === option.class
            ? 'bg-primary-100 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 ring-1 ring-primary-500'
            : 'text-surface-600 dark:text-surface-400',
        ]"
        :title="option.label"
        @click="$emit('update:modelValue', option.class)"
      >
        <i :class="[option.class, 'text-xl']"></i>
      </button>

      <div
        v-if="filteredOptions.length === 0"
        class="col-span-full py-8 text-center text-xs text-surface-500"
      >
        No icons found.
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import InputText from 'primevue/inputtext';
  import { computed, ref } from 'vue';

  interface IconOption {
    label: string;
    class: string;
  }

  interface Props {
    modelValue: string | null;
    options: IconOption[];
    label?: string;
    disabled?: boolean;
    hasHiddenIcons?: boolean;
    hiddenMessage?: string;
  }

  const props = withDefaults(defineProps<Props>(), {
    modelValue: null,
    options: () => [],
    label: 'Role Icon',
    disabled: false,
    hasHiddenIcons: false,
    hiddenMessage: 'Some icons are in use by other roles.',
  });

  defineEmits<{
    (e: 'update:modelValue', value: string): void;
  }>();

  const searchQuery = ref('');

  const filteredOptions = computed(() => {
    if (!searchQuery.value) return props.options;
    const query = searchQuery.value.toLowerCase();
    return props.options.filter((opt) => opt.label.toLowerCase().includes(query));
  });
</script>

<style scoped>
  .custom-scrollbar::-webkit-scrollbar {
    width: 6px;
  }
  .custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: var(--surface-300);
    border-radius: 20px;
  }
  .dark .custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: var(--surface-700);
  }
</style>
