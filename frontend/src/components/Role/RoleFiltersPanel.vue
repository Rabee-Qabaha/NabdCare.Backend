<template>
  <Dialog v-model:visible="visible" header="Filters" modal :style="{ width: '450px' }">
    <div class="flex flex-col gap-4">
      <!-- Name -->
      <div>
        <label class="block mb-1 text-sm font-medium">Role Name</label>
        <InputText v-model="filters.search" placeholder="Search..." class="w-full" />
      </div>

      <!-- Type -->
      <div>
        <label class="block mb-1 text-sm font-medium">Role Type</label>
        <Select
          v-model="filters.type"
          :options="[
            { label: 'System Role', value: 'system' },
            { label: 'Clinic Role', value: 'clinic' },
          ]"
          placeholder="Select..."
          class="w-full"
          showClear
        />
      </div>

      <!-- Status -->
      <div>
        <label class="block mb-1 text-sm font-medium">Status</label>
        <Select
          v-model="filters.status"
          :options="[
            { label: 'Active', value: 'active' },
            { label: 'Deleted', value: 'deleted' },
          ]"
          placeholder="Select..."
          class="w-full"
          showClear
        />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-between">
        <Button label="Clear" outlined @click="clear" />
        <Button label="Apply" icon="pi pi-check" @click="apply" />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import { computed, reactive } from 'vue';

  const props = defineProps<{ visible: boolean }>();
  const emit = defineEmits(['update:visible', 'apply', 'clear']);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  const filters = reactive({
    search: '',
    type: null,
    status: null,
  });

  function clear() {
    filters.search = '';
    filters.type = null;
    filters.status = null;
    emit('clear');
  }

  function apply() {
    emit('apply', { ...filters });
  }
</script>
