<script setup lang="ts">
  import BranchFormSidebar from '@/components/Branches/BranchFormSidebar.vue';
  import { useBranchActions } from '@/composables/query/branches/useBranchActions';
  import { useClinicBranches } from '@/composables/query/branches/useBranches';
  import { useToastService } from '@/composables/useToastService';
  import type { BranchResponseDto, ClinicResponseDto } from '@/types/backend';
  import { useConfirm } from 'primevue/useconfirm';
  import { computed, ref } from 'vue';

  // PrimeVue Components
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import ConfirmPopup from 'primevue/confirmpopup';
  import DataTable from 'primevue/datatable';
  import Dialog from 'primevue/dialog';
  import Tag from 'primevue/tag';

  const props = defineProps<{
    visible: boolean;
    clinic: ClinicResponseDto | null;
  }>();

  const emit = defineEmits(['update:visible']);
  const confirm = useConfirm();
  const toast = useToastService();

  // -- State --
  const isFormVisible = ref(false);
  const selectedBranch = ref<BranchResponseDto | null>(null);

  // -- Queries --
  const { data: branches, isLoading } = useClinicBranches(computed(() => props.clinic?.id || ''));

  const { deleteMutation, toggleStatusMutation, canCreate, canEdit, canDelete, canToggleStatus } =
    useBranchActions();

  // -- Computed --
  const sortedBranches = computed(() => {
    return [...(branches.value || [])].sort((a, b) => {
      if (a.isMain) return -1;
      if (b.isMain) return 1;
      return a.name.localeCompare(b.name);
    });
  });

  const branchCount = computed(() => branches.value?.length || 0);

  // -- Actions --
  const openCreate = () => {
    selectedBranch.value = null;
    isFormVisible.value = true;
  };

  const openEdit = (branch: BranchResponseDto) => {
    selectedBranch.value = branch;
    isFormVisible.value = true;
  };

  const onToggleStatus = (event: Event, branch: BranchResponseDto) => {
    if (branch.isMain && branch.isActive) {
      toast.warn('Cannot deactivate HQ. Set another branch as Main first.');
      return;
    }

    confirm.require({
      target: event.currentTarget as HTMLElement,
      message: `Are you sure you want to ${branch.isActive ? 'close' : 'open'} this branch?`,
      icon: 'pi pi-info-circle',
      acceptClass: branch.isActive ? 'p-button-danger' : 'p-button-success',
      accept: () => toggleStatusMutation.mutate(branch.id),
    });
  };

  const onDelete = (event: Event, branch: BranchResponseDto) => {
    confirm.require({
      target: event.currentTarget as HTMLElement,
      message: 'This action cannot be undone. Delete branch?',
      icon: 'pi pi-trash',
      acceptClass: 'p-button-danger',
      accept: () => deleteMutation.mutate(branch.id),
    });
  };
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :dismissable-mask="false"
    :style="{ width: '900px', maxWidth: '95vw' }"
    :draggable="false"
    :header="clinic?.name"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex flex-col gap-1">
        <h2 class="text-xl font-bold text-surface-900 dark:text-surface-0">Branch Management</h2>
        <span class="text-sm text-surface-500 dark:text-surface-400">
          Manage locations, contact info, and operational status.
        </span>
      </div>
    </template>

    <div class="flex flex-col h-[65vh] md:h-[600px]">
      <div class="flex justify-between items-center mb-4 px-1">
        <div class="flex gap-3 items-center">
          <div
            class="bg-surface-100 dark:bg-surface-800 px-3 py-1 rounded-md text-sm font-medium text-surface-600 dark:text-surface-300 border border-surface-200 dark:border-surface-700"
          >
            <i class="pi pi-building mr-2 text-primary"></i>
            {{ branchCount }} Location{{ branchCount !== 1 ? 's' : '' }}
          </div>
        </div>

        <Button
          v-if="canCreate"
          label="Add Branch"
          icon="pi pi-plus"
          size="small"
          severity="primary"
          raised
          @click="openCreate"
        />
      </div>

      <div
        class="flex-1 overflow-hidden border border-surface-200 dark:border-surface-700 rounded-xl shadow-sm bg-surface-0 dark:bg-surface-900"
      >
        <DataTable
          :value="sortedBranches"
          :loading="isLoading"
          scrollable
          scroll-height="flex"
          row-hover
          data-key="id"
          class="p-datatable-sm"
        >
          <template #empty>
            <div class="flex flex-col items-center justify-center h-64 text-surface-500 gap-3">
              <i class="pi pi-building text-4xl opacity-20"></i>
              <span>No branches found for this clinic.</span>
            </div>
          </template>

          <Column header="Branch" style="min-width: 250px">
            <template #body="{ data }">
              <div class="flex items-start gap-3 py-1">
                <div
                  class="w-10 h-10 rounded-full flex items-center justify-center shrink-0 border"
                  :class="[
                    data.isMain
                      ? 'bg-orange-50 border-orange-200 text-orange-600 dark:bg-orange-900/20 dark:border-orange-800 dark:text-orange-400'
                      : 'bg-surface-50 border-surface-200 text-surface-500 dark:bg-surface-800 dark:border-surface-700 dark:text-surface-400',
                  ]"
                >
                  <i :class="data.isMain ? 'pi pi-star-fill' : 'pi pi-map-marker'"></i>
                </div>

                <div class="flex flex-col">
                  <div class="flex items-center gap-2">
                    <span class="font-semibold text-surface-900 dark:text-surface-0">
                      {{ data.name }}
                    </span>
                    <Tag
                      v-if="data.isMain"
                      value="HQ"
                      severity="warning"
                      class="!text-[10px] !px-1.5 !py-0.5"
                    />
                  </div>
                  <span class="text-xs text-surface-500 truncate max-w-[200px]">
                    {{ data.address || 'No address provided' }}
                  </span>
                </div>
              </div>
            </template>
          </Column>

          <Column header="Contact" style="min-width: 200px">
            <template #body="{ data }">
              <div class="flex flex-col gap-1 text-sm text-surface-600 dark:text-surface-300">
                <div v-if="data.phone" class="flex items-center gap-2">
                  <i class="pi pi-phone text-xs opacity-70"></i>
                  {{ data.phone }}
                </div>
                <div v-if="data.email" class="flex items-center gap-2">
                  <i class="pi pi-envelope text-xs opacity-70"></i>
                  {{ data.email }}
                </div>
                <span
                  v-if="!data.phone && !data.email"
                  class="text-xs text-surface-400 italic opacity-60"
                >
                  No contact info
                </span>
              </div>
            </template>
          </Column>

          <Column header="Status" style="width: 120px">
            <template #body="{ data }">
              <Tag
                :value="data.isActive ? 'Active' : 'Closed'"
                :severity="data.isActive ? 'success' : 'danger'"
                class="uppercase !text-[10px] !tracking-wider !font-bold"
              />
            </template>
          </Column>

          <Column header="" align-frozen="right" frozen style="width: 100px">
            <template #body="{ data }">
              <div class="flex justify-end gap-1">
                <Button
                  v-if="canToggleStatus"
                  v-tooltip.left="data.isActive ? 'Close Branch' : 'Re-open Branch'"
                  :icon="data.isActive ? 'pi pi-power-off' : 'pi pi-refresh'"
                  text
                  rounded
                  size="small"
                  :severity="data.isActive ? 'secondary' : 'success'"
                  :disabled="data.isMain && data.isActive"
                  class="w-8 h-8"
                  @click="onToggleStatus($event, data)"
                />

                <Button
                  v-if="canEdit"
                  v-tooltip.top="'Edit Details'"
                  icon="pi pi-pencil"
                  text
                  rounded
                  size="small"
                  severity="info"
                  class="w-8 h-8"
                  @click="openEdit(data)"
                />

                <Button
                  v-if="canDelete"
                  v-tooltip.top="'Delete'"
                  icon="pi pi-trash"
                  text
                  rounded
                  size="small"
                  severity="danger"
                  :disabled="data.isMain"
                  class="w-8 h-8"
                  @click="onDelete($event, data)"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>
    </div>

    <BranchFormSidebar
      v-model:visible="isFormVisible"
      :clinic-id="clinic?.id || ''"
      :branch-to-edit="selectedBranch"
    />

    <ConfirmPopup />
  </Dialog>
</template>
