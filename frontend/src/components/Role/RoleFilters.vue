<template>
  <Dialog v-model:visible="visible" header="Filters" modal :style="{ width: '500px' }">
    <div class="grid gap-4">
      <!-- Role Name -->
      <div>
        <label class="mb-2 block text-sm font-medium">Role Name</label>
        <InputText v-model="local.search" placeholder="Search by role name..." class="w-full" />
      </div>

      <!-- Type -->
      <div>
        <label class="mb-2 block text-sm font-medium">Type</label>
        <Select
          v-model="local.type"
          :options="[
            { label: 'System Role', value: true },
            { label: 'Clinic Role', value: false },
          ]"
          optionLabel="label"
          optionValue="value"
          placeholder="Select type..."
          showClear
          class="w-full"
        />
      </div>

      <!-- Status -->
      <div>
        <label class="mb-2 block text-sm font-medium">Status</label>
        <Select
          v-model="local.deleted"
          :options="[
            { label: 'Active', value: false },
            { label: 'Deleted', value: true },
          ]"
          optionLabel="label"
          optionValue="value"
          placeholder="Select status..."
          showClear
          class="w-full"
        />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-between gap-2">
        <Button label="Clear" icon="pi pi-filter-slash" outlined @click="onClear" />
        <Button label="Apply" icon="pi pi-check" @click="apply" />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { reactive, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    search: string;
    type: boolean | null;
    deleted: boolean | null;
  }>();

  const emit = defineEmits(['update:visible', 'apply', 'reset']);

  const visible = defineModel<boolean>('visible');

  const local = reactive({
    search: props.search,
    type: props.type,
    deleted: props.deleted,
  });

  // Reset when reopening
  watch(
    () => props.visible,
    (v) => {
      if (v) {
        local.search = props.search;
        local.type = props.type;
        local.deleted = props.deleted;
      }
    },
  );

  const apply = () => {
    emit('apply', { ...local });
    visible.value = false;
  };

  const onClear = () => {
    emit('reset');
    visible.value = false;
  };
</script>
