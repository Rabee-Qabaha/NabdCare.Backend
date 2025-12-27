// src/components/Dropdowns/ClinicSelect.vue
<script setup lang="ts">
  import FloatLabel from 'primevue/floatlabel';
  import Select from 'primevue/select';
  import { computed } from 'vue';

  import { useClinics } from '@/composables/query/useDropdownData';

  const props = defineProps<{
    modelValue: string | null;
    label?: string;
    disabled?: boolean;
    showLabel?: boolean;
    invalid?: boolean;
    required?: boolean;
    valueKey?: 'id' | 'name';
    showClear?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
  }>();

  const { data: clinicsData, isLoading, error } = useClinics();

  const clinics = computed(() => clinicsData.value ?? []);

  const selectedClinic = computed(() => {
    const key = props.valueKey ?? 'id';
    return clinics.value.find((c) => c[key] === props.modelValue) ?? null;
  });
</script>

<template>
  <FloatLabel variant="on" class="w-full">
    <Select
      size="medium"
      class="w-full"
      :modelValue="modelValue"
      @update:modelValue="emit('update:modelValue', $event)"
      :options="clinics"
      optionLabel="name"
      :optionValue="valueKey ?? 'id'"
      :loading="isLoading"
      filter
      filterPlaceholder="Search clinics..."
      :disabled="disabled || !!error"
      :invalid="invalid"
      :showClear="showClear ?? true"
    >
      <!-- ⭐ FIXED VALUE SLOT (No 'value' used) -->
      <template #value>
        <div v-if="selectedClinic" class="flex items-center gap-2">
          <i class="pi pi-building text-primary"></i>
          <span class="truncate">{{ selectedClinic.name }}</span>
        </div>
      </template>

      <!-- ⭐ YOUR ORIGINAL OPTION TEMPLATE (unchanged) -->
      <template #option="{ option }">
        <div class="flex flex-col gap-1">
          <div class="flex items-center gap-2">
            <i class="pi pi-building text-green-500"></i>
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
