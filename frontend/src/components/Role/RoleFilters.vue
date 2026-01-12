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
          <InputText v-model="local.global" placeholder="Name or Description..." class="w-full" />
        </IconField>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Role Origin</label>
        <Select
          v-model="local.roleOrigin"
          :options="originOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="All Origins"
          showClear
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
        <label class="text-sm font-semibold text-surface-700">Role Usage</label>
        <Select
          v-model="local.isTemplate"
          :options="templateOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="All Types"
          showClear
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
          optionLabel="label"
          optionValue="value"
          placeholder="Active Only (Default)"
          class="w-full"
        >
          <template #option="slotProps">
            <div class="flex items-center gap-2">
              <i
                :class="[
                  slotProps.option.icon,
                  slotProps.option.value === 'deleted'
                    ? 'text-red-500'
                    : slotProps.option.value === 'all'
                      ? 'text-blue-500'
                      : 'text-green-500',
                ]"
              ></i>
              <span>{{ slotProps.option.label }}</span>
            </div>
          </template>
        </Select>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Created Date</label>
        <DatePicker
          v-model="local.dateRange"
          selectionMode="range"
          :manualInput="false"
          placeholder="Select date range"
          showIcon
          fluid
          class="w-full"
          dateFormat="yy-mm-dd"
          showButtonBar
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
          @click="onClear"
          class="w-full"
        />
      </div>
    </template>
  </Drawer>
</template>

<script setup lang="ts">
  import Button from 'primevue/button';
  import DatePicker from 'primevue/datepicker';
  import Drawer from 'primevue/drawer';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import { computed, reactive, watch } from 'vue';

  // ✅ UPDATED INTERFACE to match 'useRoleFilters.ts'
  interface FilterState {
    global: string;
    roleOrigin: string | null; // Changed from isSystem (boolean)
    isTemplate: boolean | null;
    status: string | null;
    dateRange: Date[] | null;
  }

  const props = defineProps<{
    visible: boolean;
    filters: FilterState;
  }>();

  const emit = defineEmits(['update:visible', 'apply', 'reset']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  // Local state for the drawer inputs
  const local = reactive<FilterState>({
    global: '',
    roleOrigin: null,
    isTemplate: null,
    status: 'active',
    dateRange: null,
  });

  // ✅ UPDATED OPTIONS (String values for Backend DTO)
  const originOptions = [
    { label: 'System Roles', value: 'system', icon: 'pi pi-lock' },
    { label: 'Clinic Roles', value: 'clinic', icon: 'pi pi-building' },
  ];

  const templateOptions = [
    { label: 'Templates', value: true, icon: 'pi pi-copy' },
    { label: 'Standard Roles', value: false, icon: 'pi pi-id-card' },
  ];

  const statusOptions = [
    { label: 'Active Only', value: 'active', icon: 'pi pi-check-circle' },
    { label: 'Deleted Only', value: 'deleted', icon: 'pi pi-trash' },
    { label: 'Show All', value: 'all', icon: 'pi pi-list' },
  ];

  // Sync prop -> local when drawer opens
  watch(
    () => props.visible,
    (isOpen) => {
      if (isOpen) {
        Object.assign(local, props.filters);
      }
    },
  );

  // Instant Apply with Debounce
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
    local.roleOrigin = null;
    local.isTemplate = null;
    local.status = 'active';
    local.dateRange = null;
  };
</script>
