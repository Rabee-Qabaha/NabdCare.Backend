<script setup lang="ts">
import { computed } from "vue";
import Select from "primevue/select";
import { useClinics } from "@/composables/query/useDropdownData";

const props = defineProps<{
  modelValue: string | null;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  showLabel?: boolean;
  invalid?: boolean;
  required?: boolean;
  valueKey?: "id" | "name";
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: string | null): void;
}>();

const { data: clinicsData, isLoading, error } = useClinics();

// ✅ Prevent undefined
const clinics = computed(() => clinicsData.value ?? []);

// ✅ Match selected clinic using chosen key
const selectedClinic = computed(() => {
  const key = props.valueKey ?? "id"; // default to id
  return clinics.value.find((c) => c[key] === props.modelValue) ?? null;
});
</script>

<template>
  <div class="flex flex-col">
    <label v-if="showLabel" class="block font-medium mb-2"
      >{{ label || "Clinic" }}
      <span v-if="props.required" class="text-red-500">*</span>
    </label>

    <Select
      :modelValue="modelValue"
      @update:modelValue="$emit('update:modelValue', $event)"
      :options="clinics"
      optionLabel="name"
      :optionValue="props.valueKey ?? 'id'"
      :placeholder="placeholder || 'Select a clinic'"
      :loading="isLoading"
      showClear
      class="w-full"
      filter
      filterPlaceholder="Search clinics..."
      :disabled="disabled || !!error"
      :invalid="invalid"
    >
      <template #value="{ value }">
        <div v-if="value" class="flex items-center gap-2">
          <i class="pi pi-building text-primary"></i>
          <span
            class="flex items-center gap-1 text-surface-600 dark:text-surface-400"
          >
            {{ selectedClinic?.name || "Unknown" }}
          </span>
        </div>
        <span v-else class="text-surface-500">Select a clinic</span>
      </template>

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
  </div>
</template>
