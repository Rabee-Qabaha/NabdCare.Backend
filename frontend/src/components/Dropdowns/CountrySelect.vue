<script setup lang="ts">
  import { COUNTRIES } from '@/constants/countries';
  import Select from 'primevue/select';
  import { computed } from 'vue';

  const props = defineProps<{
    modelValue?: string;
    placeholder?: string;
    class?: string;
  }>();

  const emit = defineEmits(['update:modelValue']);

  const selectedCountry = computed(() => COUNTRIES.find((c) => c.name === props.modelValue));

  const onUpdate = (val: string) => {
    emit('update:modelValue', val);
  };
</script>

<template>
  <Select
    :modelValue="modelValue"
    :options="COUNTRIES"
    optionLabel="name"
    optionValue="name"
    :placeholder="placeholder || 'Select a Country'"
    filter
    class="w-full custom-country-select"
    :class="props.class"
    @update:modelValue="onUpdate"
  >
    <template #value="slotProps">
      <div v-if="slotProps.value" class="flex items-center gap-3 h-full">
        <img
          v-if="selectedCountry"
          :src="`https://flagcdn.com/w40/${selectedCountry.code.toLowerCase()}.png`"
          :alt="selectedCountry.name"
          class="shrink-0 flag-icon"
        />
        <span class="text-sm font-medium leading-none">{{ slotProps.value }}</span>
      </div>
      <label v-else class="text-surface-500 dark:text-surface-400">
        {{ slotProps.placeholder }}
      </label>
    </template>

    <template #option="slotProps">
      <div class="flex items-center gap-3 py-0">
        <img
          :src="`https://flagcdn.com/w40/${slotProps.option.code.toLowerCase()}.png`"
          :alt="slotProps.option.name"
          class="shrink-0 flag-icon"
        />
        <span class="text-sm">{{ slotProps.option.name }}</span>
      </div>
    </template>
  </Select>
</template>

<style scoped>
  /**
 * 🛠️ Visual Styling for Flags
 */
  .flag-icon {
    width: 24px; /* Your requested size */
    height: auto;
    border-radius: 2px;
    box-shadow: 0 0 2px rgba(0, 0, 0, 0.15);
    /* Ensures no distortion */
    object-fit: contain;
  }

  /**
 * 🛠️ Height Consistency Fix
 * Keeps the selector at standard PrimeVue height (40px / 2.5rem)
 */
  .custom-country-select :deep(.p-select-label) {
    padding-top: 0 !important;
    padding-bottom: 0 !important;
    display: flex;
    align-items: center;
    height: 2.5rem;
  }

  /* Vertical spacing for options in the dropdown list */
  :deep(.p-select-option) {
    padding-top: 0.5rem !important;
    padding-bottom: 0.5rem !important;
  }

  :deep(.p-select-filter) {
    font-size: 0.875rem;
  }
</style>
