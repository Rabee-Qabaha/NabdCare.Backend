<template>
  <Drawer
    v-model:visible="visible"
    header="Advanced Filters"
    position="right"
    class="!w-80 md:!w-96"
  >
    <div class="flex flex-col gap-6">
      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Search</label>
        <IconField>
          <InputIcon class="pi pi-search" />
          <InputText v-model="local.global" placeholder="Name, Email, or Phone..." class="w-full" />
        </IconField>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Status</label>
        <Select
          v-model="local.status"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          placeholder="Any Status"
          show-clear
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
          option-label="label"
          option-value="value"
          placeholder="Any Plan"
          show-clear
          class="w-full"
        />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Expiration Date</label>
        <DatePicker
          v-model="local.expirationDateRange"
          selection-mode="range"
          :manual-input="false"
          placeholder="Select Date Range"
          show-icon
          fluid
          date-format="dd/mm/yy"
          show-button-bar
        />
        <small class="text-surface-500">Find clinics expiring soon.</small>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Joined Date</label>
        <DatePicker
          v-model="local.createdDateRange"
          selection-mode="range"
          :manual-input="false"
          placeholder="Select Date Range"
          show-icon
          fluid
          date-format="dd/mm/yy"
          show-button-bar
        />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Min. Branches</label>
        <InputNumber
          v-model="local.minBranches"
          show-buttons
          :min="0"
          placeholder="e.g. 5"
          class="w-full"
        />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end w-full mt-4">
        <Button
          label="Reset Filters"
          icon="pi pi-filter-slash"
          text
          severity="secondary"
          class="w-full"
          @click="onClear"
        />
      </div>
    </template>
  </Drawer>
</template>

<script setup lang="ts">
  import { SubscriptionStatus, SubscriptionType } from '@/types/backend';
  import Button from 'primevue/button';
  import DatePicker from 'primevue/datepicker';
  import Drawer from 'primevue/drawer';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputNumber from 'primevue/inputnumber';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed, reactive, watch } from 'vue';

  export interface ClinicFiltersState {
    global: string;
    status: SubscriptionStatus | number | null;
    subscriptionType: SubscriptionType | number | null;
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
    global: '',
    status: null,
    subscriptionType: null,
    minBranches: null,
    expirationDateRange: null,
    createdDateRange: null,
  });

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

  // Instant Filter Debounce
  let debounceTimer: any = null;
  watch(
    local,
    (newVal) => {
      clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        emit('apply', { ...newVal });
      }, 300);
    },
    { deep: true },
  );

  const onClear = () => {
    emit('reset');
    // Reset local
    local.global = '';
    local.status = null;
    local.subscriptionType = null;
    local.minBranches = null;
    local.expirationDateRange = null;
    local.createdDateRange = null;
  };
</script>
