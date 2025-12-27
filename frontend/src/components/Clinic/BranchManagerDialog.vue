<script setup lang="ts">
  import { useBranchActions } from '@/composables/query/branches/useBranchActions';
  import { useClinicBranches } from '@/composables/query/branches/useBranches';
  import type { BranchResponseDto, ClinicResponseDto } from '@/types/backend';
  import Button from 'primevue/button';
  import Checkbox from 'primevue/checkbox';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Dialog from 'primevue/dialog';
  import InputText from 'primevue/inputtext';
  import Tag from 'primevue/tag';
  import { ref, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    clinic: ClinicResponseDto | null;
  }>();

  const emit = defineEmits(['update:visible']);

  // -- State --
  const isEditing = ref(false);
  const editingId = ref<string | null>(null);
  const form = ref({
    name: '',
    address: '',
    phone: '',
    isMain: false,
  });

  // -- Queries --
  const { data: branches, isLoading } = useClinicBranches(props.clinic?.id || '');
  const { createMutation, updateMutation, deleteMutation, canCreate, canEdit, canDelete } =
    useBranchActions();

  // -- Actions --
  const resetForm = () => {
    isEditing.value = false;
    editingId.value = null;
    form.value = { name: '', address: '', phone: '', isMain: false };
  };

  const onEdit = (branch: BranchResponseDto) => {
    isEditing.value = true;
    editingId.value = branch.id;
    form.value = {
      name: branch.name,
      address: branch.address || '',
      phone: branch.phone || '',
      isMain: branch.isMain,
    };
  };

  const onSubmit = async () => {
    if (!props.clinic) return;

    if (isEditing.value && editingId.value) {
      await updateMutation.mutateAsync({ id: editingId.value, dto: form.value });
    } else {
      await createMutation.mutateAsync({
        clinicId: props.clinic.id,
        ...form.value,
      });
    }
    resetForm();
  };

  const onDelete = async (id: string) => {
    // Optional: Add ConfirmDialog here
    await deleteMutation.mutateAsync(id);
  };

  // Reset on open/close
  watch(
    () => props.visible,
    (val) => !val && resetForm(),
  );
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :header="`Branches: ${clinic?.name}`"
    modal
    maximizable
    class="w-full md:w-[60vw]"
  >
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-1 border-r border-surface-200 dark:border-surface-700 pr-6">
        <h3 class="text-lg font-bold mb-4">{{ isEditing ? 'Edit Branch' : 'Add New Branch' }}</h3>

        <div class="flex flex-col gap-4">
          <div class="flex flex-col gap-1">
            <label>Name</label>
            <InputText v-model="form.name" placeholder="e.g. Ramallah Center" />
          </div>
          <div class="flex flex-col gap-1">
            <label>Address</label>
            <InputText v-model="form.address" placeholder="Street, City" />
          </div>
          <div class="flex flex-col gap-1">
            <label>Phone</label>
            <InputText v-model="form.phone" />
          </div>
          <div class="flex items-center gap-2 mt-2">
            <Checkbox v-model="form.isMain" :binary="true" inputId="isMain" />
            <label for="isMain" class="cursor-pointer select-none">Main Branch (HQ)</label>
          </div>

          <div class="flex gap-2 mt-4">
            <Button
              :label="isEditing ? 'Update' : 'Create'"
              icon="pi pi-check"
              class="flex-1"
              @click="onSubmit"
              :loading="createMutation.isPending || updateMutation.isPending"
              :disabled="!canCreate && !isEditing"
            />
            <Button
              v-if="isEditing"
              label="Cancel"
              severity="secondary"
              class="flex-1"
              @click="resetForm"
            />
          </div>
        </div>
      </div>

      <div class="lg:col-span-2">
        <DataTable :value="branches" :loading="isLoading" size="small" stripedRows>
          <template #empty>No branches found.</template>

          <Column field="name" header="Name">
            <template #body="{ data }">
              <span class="font-bold">{{ data.name }}</span>
              <Tag v-if="data.isMain" value="HQ" severity="warning" class="ml-2 text-[10px]" />
            </template>
          </Column>
          <Column field="phone" header="Phone" />
          <Column header="Actions" alignFrozen="right" frozen style="width: 5rem">
            <template #body="{ data }">
              <div class="flex gap-2">
                <Button
                  icon="pi pi-pencil"
                  text
                  rounded
                  severity="info"
                  @click="onEdit(data)"
                  v-if="canEdit"
                />
                <Button
                  icon="pi pi-trash"
                  text
                  rounded
                  severity="danger"
                  @click="onDelete(data.id)"
                  :disabled="data.isMain"
                  v-if="canDelete"
                  v-tooltip.top="data.isMain ? 'Cannot delete HQ' : 'Delete'"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>
    </div>
  </Dialog>
</template>
