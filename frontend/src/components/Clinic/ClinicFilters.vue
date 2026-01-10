// src/components/Clinic/ClinicFilters.vue
<template>
  <Drawer
    v-model:visible="visible"
    header="Advanced Filters"
    position="right"
    class="!w-80 md:!w-96"
  >
    <div class="flex flex-col gap-6">
      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Status</label>
        <Select
          v-model="local.status"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Any Status"
          showClear
          class="w-full"
        >
          <template #option="slotProps">
            <Tag :severity="slotProps.option.severity" :value="slotProps.option.label" />
          </template>
        </Select>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Plan Type</label>
        <Select
          v-model="local.subscriptionType"
          :options="planOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Any Plan"
          showClear
          class="w-full"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Expiration Date</label>
        <DatePicker
          v-model="local.expirationDateRange"
          selectionMode="range"
          :manualInput="false"
          placeholder="Select Date Range"
          showIcon
          fluid
          dateFormat="dd/mm/yy"
          showButtonBar
        />
        <small class="text-surface-500">Find clinics expiring soon.</small>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Joined Date</label>
        <DatePicker
          v-model="local.createdDateRange"
          selectionMode="range"
          :manualInput="false"
          placeholder="Select Date Range"
          showIcon
          fluid
          dateFormat="dd/mm/yy"
          showButtonBar
        />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Min. Branches</label>
        <InputNumber
          v-model="local.minBranches"
          showButtons
          :min="0"
          placeholder="e.g. 5"
          class="w-full"
        />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 w-full mt-4">
        <Button
          label="Reset"
          icon="pi pi-filter-slash"
          text
          severity="secondary"
          @click="onClear"
          class="flex-1"
        />
        <Button label="Apply Filters" icon="pi pi-check" @click="onApply" class="flex-1" />
      </div>
    </template>
  </Drawer>
</template>

<script setup lang="ts">
  import { SubscriptionStatus, SubscriptionType } from '@/types/backend';
  import Button from 'primevue/button';
  import DatePicker from 'primevue/datepicker';
  import Drawer from 'primevue/drawer';
  import InputNumber from 'primevue/inputnumber';
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed, reactive, watch } from 'vue';

  // Define the flat filter structure
  export interface ClinicFiltersState {
    status: number | null;
    subscriptionType: number | null;
    minBranches: number | null;
    expirationDateRange: Date[] | null;
    createdDateRange: Date[] | null;
  }

  const props = defineProps<{
    visible: boolean;
    filters: ClinicFiltersState;
  }>();

  const emit = defineEmits(['update:visible', 'apply', 'reset']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  // Local state copy
  const local = reactive<ClinicFiltersState>({
    status: null,
    subscriptionType: null,
    minBranches: null,
    expirationDateRange: null,
    createdDateRange: null,
  });

  // Options
  const statusOptions = [
    { label: 'Active', value: SubscriptionStatus.Active, severity: 'success' },
    { label: 'Inactive', value: SubscriptionStatus.Inactive, severity: 'warn' },
    { label: 'Suspended', value: SubscriptionStatus.Suspended, severity: 'warn' },
    { label: 'Expired', value: SubscriptionStatus.Expired, severity: 'danger' },
    { label: 'Trial', value: SubscriptionStatus.Trial, severity: 'info' },
    { label: 'Cancelled', value: SubscriptionStatus.Cancelled, severity: 'secondary' },
  ];

  const planOptions = [
    { label: 'Monthly', value: SubscriptionType.Monthly },
    { label: 'Yearly', value: SubscriptionType.Yearly },
    { label: 'Lifetime', value: SubscriptionType.Lifetime },
  ];

  // Sync prop to local when opening
  watch(
    () => props.visible,
    (isOpen) => {
      if (isOpen) Object.assign(local, props.filters);
    },
  );

  const onApply = () => {
    emit('apply', { ...local });
    visible.value = false;
  };

  const onClear = () => {
    emit('reset');
    local.status = null;
    local.subscriptionType = null;
    local.minBranches = null;
    local.expirationDateRange = null;
    local.createdDateRange = null;
    visible.value = false;
  };
</script>
