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
          <InputText
            v-model="local.reference"
            placeholder="Transaction ID or Cheque No..."
            class="w-full"
          />
        </IconField>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Method</label>
        <Select
          v-model="local.method"
          :options="methodOptions"
          option-label="label"
          option-value="value"
          placeholder="Any Method"
          show-clear
          class="w-full"
        >
          <template #option="slotProps">
            <div class="flex items-center gap-2">
              <i :class="slotProps.option.icon" class="text-surface-500"></i>
              <span>{{ slotProps.option.label }}</span>
            </div>
          </template>
        </Select>
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
        <label class="text-sm font-semibold text-surface-700">Date Range</label>
        <DatePicker
          v-model="local.dateRange"
          selection-mode="range"
          :manual-input="false"
          placeholder="Select Date Range"
          show-icon
          fluid
          date-format="dd/mm/yy"
          show-button-bar
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
  import { PaymentMethod } from '@/types/backend/payment-method';
  import { PaymentStatus } from '@/types/backend/payment-status';
  import Button from 'primevue/button';
  import DatePicker from 'primevue/datepicker';
  import Drawer from 'primevue/drawer';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed, reactive, watch } from 'vue';

  export interface PaymentFiltersState {
    reference: string;
    method: PaymentMethod | null;
    status: PaymentStatus | null;
    dateRange: Date[] | null;
  }

  const props = defineProps<{
    visible: boolean;
    filters: PaymentFiltersState;
  }>();

  const emit = defineEmits(['update:visible', 'apply', 'reset']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  // Local state copy
  const local = reactive<PaymentFiltersState>({
    reference: '',
    method: null,
    status: null,
    dateRange: null,
  });

  const methodOptions = [
    { label: 'Cash', value: PaymentMethod.Cash, icon: 'pi pi-money-bill' },
    { label: 'Cheque', value: PaymentMethod.Cheque, icon: 'pi pi-ticket' },
    { label: 'Visa', value: PaymentMethod.Visa, icon: 'pi pi-credit-card' },
    { label: 'Bank Transfer', value: PaymentMethod.BankTransfer, icon: 'pi pi-building' },
  ];

  const statusOptions = [
    { label: 'Completed', value: PaymentStatus.Completed, severity: 'success' },
    { label: 'Pending', value: PaymentStatus.Pending, severity: 'warn' },
    { label: 'Failed', value: PaymentStatus.Failed, severity: 'danger' },
    { label: 'Refunded', value: PaymentStatus.Refunded, severity: 'secondary' },
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
    local.reference = '';
    local.method = null;
    local.status = null;
    local.dateRange = null;
  };
</script>
