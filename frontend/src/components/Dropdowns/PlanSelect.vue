<script setup lang="ts">
  import { usePlans } from '@/composables/query/useDropdownData';
  import FloatLabel from 'primevue/floatlabel';
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  const props = defineProps<{
    modelValue: string | null;
    label?: string;
    disabled?: boolean;
    invalid?: boolean;
    required?: boolean;
    showClear?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
    (e: 'plan-selected', plan: any): void;
  }>();

  const { data: plansData, isLoading, error } = usePlans();

  const plans = computed(() => plansData.value ?? []);

  const selectedPlan = computed(() => {
    return plans.value.find((p) => p.id === props.modelValue) ?? null;
  });

  const onUpdate = (val: string | null) => {
    emit('update:modelValue', val);
    const plan = plans.value.find((p) => p.id === val);
    if (plan) emit('plan-selected', plan);
  };

  import { useConfiguration } from '@/composables/useConfiguration';
  const { functionalCurrency: systemCurrency } = useConfiguration();

  // Helper for price formatting
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: systemCurrency.value,
    }).format(value);
  };
</script>

<template>
  <FloatLabel variant="on" class="w-full">
    <Select
      size="medium"
      class="w-full"
      :model-value="modelValue"
      :options="plans"
      option-label="name"
      option-value="id"
      append-to="self"
      :loading="isLoading"
      :disabled="disabled || !!error"
      :invalid="invalid"
      :show-clear="showClear ?? true"
      @update:model-value="onUpdate"
    >
      <template #value>
        <div v-if="selectedPlan" class="flex items-center gap-2">
          <i class="pi pi-star text-yellow-500"></i>
          <span class="font-semibold">{{ selectedPlan.name }}</span>
          <Tag :value="formatCurrency(selectedPlan.baseFee)" severity="success" class="ml-auto" />
        </div>
      </template>

      <template #option="{ option }">
        <div class="flex flex-col gap-1 w-full border-b pb-1 last:border-0 last:pb-0">
          <div class="flex justify-between items-center w-full">
            <span class="font-bold text-lg">{{ option.name }}</span>
            <span class="text-green-600 font-bold">{{ formatCurrency(option.baseFee) }}</span>
          </div>

          <div class="flex gap-2 text-xs text-surface-500 mt-1">
            <span class="flex items-center gap-1">
              <i class="pi pi-users"></i>
              {{ option.includedUsers }} Users
            </span>
            <span class="flex items-center gap-1">
              <i class="pi pi-building"></i>
              {{ option.includedBranches }} Branch
            </span>
            <span class="flex items-center gap-1">
              <i class="pi pi-clock"></i>
              {{ option.durationDays }} Days
            </span>
          </div>
        </div>
      </template>
    </Select>

    <label>
      {{ label || 'Select Plan' }}
      <span v-if="required" class="text-red-500">*</span>
    </label>
  </FloatLabel>
</template>
