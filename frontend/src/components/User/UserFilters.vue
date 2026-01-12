// src/components/User/UserFilters.vue
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
          <InputText v-model="local.global" placeholder="Name, Email..." class="w-full" />
        </IconField>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Role</label>
        <RoleSelect v-model="local.roleId" show-clear class="w-full" />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Clinic</label>
        <ClinicSelect v-model="local.clinicId" show-clear class="w-full" />
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Account Status</label>
        <Select
          v-model="local.isActive"
          :options="statusOptions"
          option-label="label"
          option-value="value"
          placeholder="All Statuses"
          show-clear
          class="w-full"
        >
          <template #option="slotProps">
            <div class="flex items-center gap-2">
              <i
                :class="[
                  slotProps.option.icon,
                  slotProps.option.value ? 'text-green-500' : 'text-orange-500',
                ]"
              ></i>
              <span>{{ slotProps.option.label }}</span>
            </div>
          </template>
        </Select>
      </div>

      <div class="flex flex-col gap-2">
        <label class="text-sm font-semibold text-surface-700">Deleted State</label>
        <Select
          v-model="local.status"
          :options="deletedOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        >
          <template #option="slotProps">
            <div class="flex items-center gap-2">
              <i
                :class="[
                  slotProps.option.icon,
                  slotProps.option.value === 'deleted' ? 'text-red-500' : 'text-surface-500',
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
          selection-mode="range"
          :manual-input="false"
          placeholder="Select date range"
          show-icon
          fluid
          class="w-full"
          date-format="yy-mm-dd"
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
  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';
  import RoleSelect from '@/components/Dropdowns/RoleSelect.vue';
  import Button from 'primevue/button';
  import DatePicker from 'primevue/datepicker';
  import Drawer from 'primevue/drawer';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import { computed, reactive, watch } from 'vue';

  // Interface matches useUserFilters.ts
  interface UserFilterState {
    global: string;
    roleId: string | null;
    clinicId: string | null;
    isActive: boolean | null;
    status: string | null;
    dateRange: Date[] | null;
  }

  const props = defineProps<{
    visible: boolean;
    filters: UserFilterState;
  }>();

  const emit = defineEmits(['update:visible', 'apply', 'reset']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  const local = reactive<UserFilterState>({
    global: '',
    roleId: null,
    clinicId: null,
    isActive: null,
    status: 'active',
    dateRange: null,
  });

  const statusOptions = [
    { label: 'Active Users', value: true, icon: 'pi pi-check-circle' },
    { label: 'Deactivated Users', value: false, icon: 'pi pi-ban' },
  ];

  const deletedOptions = [
    { label: 'Active Only', value: 'active', icon: 'pi pi-user' },
    { label: 'Deleted Only', value: 'deleted', icon: 'pi pi-trash' },
    { label: 'Show All', value: 'all', icon: 'pi pi-list' },
  ];

  // Sync
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
    local.roleId = null;
    local.clinicId = null;
    local.isActive = null;
    local.status = 'active';
    local.dateRange = null;
  };
</script>
