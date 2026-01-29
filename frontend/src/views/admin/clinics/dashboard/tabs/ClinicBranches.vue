<template>
  <div
    class="card bg-surface-0 dark:bg-surface-900 p-6 rounded-2xl border border-surface-200 dark:border-surface-800"
  >
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
      <div class="flex items-center gap-3">
        <div
          class="w-10 h-10 rounded-lg bg-primary-50 dark:bg-primary-900/20 flex items-center justify-center text-primary-600 dark:text-primary-400"
        >
          <i class="pi pi-building text-xl"></i>
        </div>
        <div>
          <h2 class="text-lg font-bold text-surface-900 dark:text-surface-0">Branches</h2>
          <p class="text-sm text-surface-500 dark:text-surface-400">Manage clinic branches</p>
        </div>
      </div>

      <div class="flex flex-wrap gap-2 w-full sm:w-auto">
        <IconField iconPosition="left" class="w-full sm:w-64">
          <InputIcon class="pi pi-search" />
          <InputText
            v-model="filters.global.value"
            placeholder="Search branches..."
            class="w-full"
          />
        </IconField>

        <keep-alive>
          <SelectButton
            v-model="statusFilter"
            :options="statusOptions"
            optionLabel="label"
            optionValue="value"
            class="p-selectbutton-sm"
          />
        </keep-alive>

        <Button
          v-if="canCreate"
          label="Add Branch"
          icon="pi pi-plus"
          size="small"
          @click="openCreate"
        />
      </div>
    </div>

    <div v-if="isLoading" class="space-y-4">
      <Skeleton v-for="i in 3" :key="i" height="8rem" borderRadius="1rem" />
    </div>

    <div v-else-if="filteredBranches.length > 0" class="flex flex-col gap-4">
      <BranchCard
        v-for="branch in filteredBranches"
        :key="branch.id"
        :branch="branch"
        :is-toggling="isToggling(branch.id)"
        @edit="onEdit"
        @toggle-status="onToggleStatus"
        @delete="onDelete"
      />
    </div>

    <div
      v-else
      class="text-center p-12 bg-surface-50 dark:bg-surface-800/50 rounded-2xl border border-dashed border-surface-200 dark:border-surface-700"
    >
      <div class="flex flex-col items-center gap-3">
        <div
          class="w-16 h-16 rounded-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center mb-2"
        >
          <i class="pi pi-search text-2xl text-surface-400"></i>
        </div>
        <h3 class="text-lg font-medium text-surface-900 dark:text-surface-0">No branches found</h3>
        <p class="text-surface-500 dark:text-surface-400 max-w-xs">
          We couldn't find any branches matching your search filters.
        </p>
        <Button
          label="Clear Filters"
          severity="secondary"
          outlined
          size="small"
          class="mt-2"
          @click="clearFilters"
        />
      </div>
    </div>

    <BranchFormSidebar
      v-model:visible="isFormVisible"
      :clinic-id="clinicId"
      :branch-to-edit="selectedBranch"
      @saved="onSaved"
    />

    <ConfirmDialog />
  </div>
</template>

<script setup lang="ts">
  import BranchFormSidebar from '@/components/Branches/BranchFormSidebar.vue';
  import BranchCard from '@/components/Subscription/profile/Branch/BranchCard.vue';
  import { useBranchActions } from '@/composables/query/branches/useBranchActions';
  import { useClinicBranches } from '@/composables/query/branches/useBranches';
  import { useToastService } from '@/composables/useToastService';
  import type { BranchResponseDto } from '@/types/backend';
  import { FilterMatchMode } from '@primevue/core/api';
  import Button from 'primevue/button';
  import ConfirmDialog from 'primevue/confirmdialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import SelectButton from 'primevue/selectbutton';
  import Skeleton from 'primevue/skeleton';
  import { useConfirm } from 'primevue/useconfirm';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    clinicId: string;
  }>();

  const confirm = useConfirm();
  const toast = useToastService();
  const { data: branches, isLoading, refetch } = useClinicBranches(computed(() => props.clinicId));
  const { toggleStatusMutation, deleteMutation, canCreate } = useBranchActions();

  // Sidebar State
  const isFormVisible = ref(false);
  const selectedBranch = ref<BranchResponseDto | null>(null);

  // Filtering
  const filters = ref({
    global: { value: null, matchMode: FilterMatchMode.CONTAINS },
  });

  const statusFilter = ref<'all' | 'active' | 'inactive'>('all');
  const statusOptions = [
    { label: 'All', value: 'all' },
    { label: 'Active', value: 'active' },
    { label: 'Inactive', value: 'inactive' },
  ];

  const clearFilters = () => {
    filters.value.global.value = null;
    statusFilter.value = 'all';
  };

  const filteredBranches = computed(() => {
    if (!branches.value) return [];
    let items = branches.value;

    // Filter by Status
    if (statusFilter.value !== 'all') {
      const isActive = statusFilter.value === 'active';
      items = items.filter((b) => b.isActive === isActive);
    }

    // Client-side global search
    if (filters.value.global.value) {
      const query = (filters.value.global.value as string).toLowerCase();
      items = items.filter(
        (b) =>
          b.name.toLowerCase().includes(query) ||
          b.phone?.includes(query) ||
          b.email?.toLowerCase().includes(query) ||
          b.address?.toLowerCase().includes(query),
      );
    }

    return items;
  });

  // Actions
  const openCreate = () => {
    selectedBranch.value = null;
    isFormVisible.value = true;
  };

  const onEdit = (branch: BranchResponseDto) => {
    selectedBranch.value = branch;
    isFormVisible.value = true;
  };

  const onSaved = () => {
    refetch(); // Refresh list after save (though useBranchActions usually handles cache invalidation)
  };

  const isToggling = (id: string) =>
    toggleStatusMutation.isPending.value && toggleStatusMutation.variables.value === id;

  const onToggleStatus = (branch: BranchResponseDto) => {
    if (branch.isMain && branch.isActive) {
      toast.warn('Cannot deactivate HQ. Set another branch as Main first.');
      return;
    }

    confirm.require({
      message: `Are you sure you want to ${branch.isActive ? 'deactivate' : 'activate'} "${branch.name}"?`,
      header: 'Confirm Status Change',
      icon: 'pi pi-exclamation-triangle',
      rejectProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true,
      },
      acceptProps: {
        label: branch.isActive ? 'Deactivate' : 'Activate',
        severity: branch.isActive ? 'warn' : 'success',
      },
      accept: () => {
        toggleStatusMutation.mutate(branch.id);
      },
    });
  };

  const onDelete = (branch: BranchResponseDto) => {
    confirm.require({
      message: `Are you sure you want to delete "${branch.name}"? This action cannot be undone.`,
      header: 'Delete Branch',
      icon: 'pi pi-trash',
      rejectProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true,
      },
      acceptProps: {
        label: 'Delete',
        severity: 'danger',
      },
      accept: () => {
        deleteMutation.mutate(branch.id);
      },
    });
  };
</script>
