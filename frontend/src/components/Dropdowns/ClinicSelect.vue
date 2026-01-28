// src/components/Dropdowns/ClinicSelect.vue
<script setup lang="ts">
  import FloatLabel from 'primevue/floatlabel';
  import Select from 'primevue/select';
  import { computed, watch } from 'vue';

  import { useClinics } from '@/composables/query/useDropdownData';

  const props = withDefaults(
    defineProps<{
      modelValue: string | null;
      label?: string;
      disabled?: boolean;
      showLabel?: boolean;
      invalid?: boolean;
      required?: boolean;
      valueKey?: 'id' | 'name';
      showClear?: boolean;
      clearOnDisabled?: boolean; // Added prop
    }>(),
    {
      showClear: true,
      clearOnDisabled: false,
    },
  );

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
  }>();

  const { data: clinicsData, isLoading, error } = useClinics();

  const clinics = computed(() => clinicsData.value ?? []);

  const selectedClinic = computed(() => {
    const key = props.valueKey ?? 'id';
    return clinics.value.find((c) => c[key] === props.modelValue) ?? null;
  });

  watch(
    () => props.disabled,
    (isDisabled) => {
      if (isDisabled && props.clearOnDisabled) {
        emit('update:modelValue', null);
      }
    },
  );
</script>

<template>
  <FloatLabel variant="on" class="w-full">
    <Select
      size="medium"
      class="w-full"
      :model-value="modelValue"
      :options="clinics"
      option-label="name"
      :option-value="valueKey ?? 'id'"
      :loading="isLoading"
      filter
      append-to="self"
      :show-clear="showClear"
      filter-placeholder="Search clinics..."
      :disabled="disabled || !!error"
      :invalid="invalid"
      @update:model-value="emit('update:modelValue', $event)"
    >
      <!-- ⭐ FIXED VALUE SLOT (No 'value' used) -->
      <template #value>
        <div v-if="selectedClinic" class="flex items-center gap-2">
          <i class="pi pi-building text-primary-500"></i>
          <span class="truncate">{{ selectedClinic.name }}</span>
        </div>
      </template>

      <!-- ⭐ YOUR ORIGINAL OPTION TEMPLATE (unchanged) -->
      <template #option="{ option }">
        <div class="flex flex-col gap-1">
          <div class="flex items-center gap-2">
            <i class="pi pi-building text-primary-500"></i>
            <span class="font-semibold">{{ option.name }}</span>
          </div>
          <small class="text-surface-500">{{ option.email }}</small>
        </div>
      </template>
    </Select>

    <!-- Floating label -->
    <label>
      {{ label || 'Select a Clinic' }}
      <span v-if="required" class="text-red-500">*</span>
    </label>
  </FloatLabel>
</template>
