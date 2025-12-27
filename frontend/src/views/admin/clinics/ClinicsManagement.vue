<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <header>
        <PageHeader
          title="Clinics Management"
          description="Manage tenants, subscriptions, and branch structures."
          :stats="[{ icon: 'pi pi-building', label: `${totalRecords} Clinics` }]"
        />
      </header>

      <ClinicToolbar
        :loading="loading"
        :include-deleted="includeDeleted"
        :can-create="canCreate"
        @create="openCreateDialog"
        @toggle-deleted="toggleDeletedView"
        @refresh="refresh"
        @clear-filters="clearFilters"
        @filters="showFilters = true"
      />

      <div>
        <IconField class="w-full">
          <InputIcon><i class="pi pi-search" /></InputIcon>
          <InputText
            v-model="filters.global"
            placeholder="Search clinics by name, email, phone or subdomain..."
            class="w-full"
          />
        </IconField>
      </div>

      <ClinicTable
        :virtual-clinics="virtualClinics"
        :loading="loading"
        v-model:filters="filters"
        @lazy-load="loadCarsLazy"
        @sort="onSort"
        @refresh="refresh"
        @action="handleTableAction"
      />

      <ClinicFilters
        v-model:visible="showFilters"
        :filters="filters"
        @apply="applyFilters"
        @reset="clearFilters"
      />

      <ClinicDialog v-model:visible="dialogVisible" :clinic="selectedClinic" @save="handleSave" />

      <SubscriptionDialog
        v-if="selectedClinic"
        v-model:visible="showSubscriptionDialog"
        :clinic="selectedClinic"
      />

      <BranchManagerDialog
        v-if="selectedClinic"
        v-model:visible="showBranchDialog"
        :clinic="selectedClinic"
      />

      <ConfirmDialog />
    </div>
  </div>
</template>

<script setup lang="ts">
  import { useClinicActions } from '@/composables/query/clinics/useClinicActions';
  import { useClinicsTable } from '@/composables/query/clinics/useClinicsTable';
  // ✅ 1. Import Enum for Type-Safe Comparisons
  import { SubscriptionStatus, type ClinicResponseDto } from '@/types/backend';
  import { useConfirm } from 'primevue/useconfirm';
  import { ref } from 'vue';

  // Components
  import BranchManagerDialog from '@/components/Clinic/BranchManagerDialog.vue';
  import ClinicDialog from '@/components/Clinic/ClinicDialog.vue';
  import ClinicFilters from '@/components/Clinic/ClinicFilters.vue';
  import ClinicTable from '@/components/Clinic/ClinicTable.vue';
  import ClinicToolbar from '@/components/Clinic/ClinicToolbar.vue';
  import SubscriptionDialog from '@/components/Subscription/SubscriptionDialog.vue';
  import PageHeader from '@/layout/PageHeader.vue';

  import ConfirmDialog from 'primevue/confirmdialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';

  // State
  const includeDeleted = ref(false);
  const dialogVisible = ref(false);
  const showFilters = ref(false);

  // ✅ New Dialog States
  const showSubscriptionDialog = ref(false);
  const showBranchDialog = ref(false);

  const selectedClinic = ref<ClinicResponseDto | null>(null);

  // Virtual Table Composable
  const {
    virtualClinics,
    totalRecords,
    loading,
    filters,
    loadCarsLazy,
    onSort,
    refresh,
    clearFilters,
    applyFilters,
  } = useClinicsTable(includeDeleted);

  // Actions
  const {
    createClinicMutation,
    updateClinicMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
    activateMutation,
    suspendMutation,
    canCreate,
  } = useClinicActions();

  const confirm = useConfirm();

  // --- Handlers ---

  function openCreateDialog() {
    selectedClinic.value = null;
    dialogVisible.value = true;
  }

  function openEditDialog(clinic: ClinicResponseDto) {
    selectedClinic.value = clinic;
    dialogVisible.value = true;
  }

  function toggleDeletedView() {
    includeDeleted.value = !includeDeleted.value;
    refresh();
  }

  async function handleSave(payload: any) {
    if (payload.id) {
      await updateClinicMutation.mutateAsync({ id: payload.id, data: payload });
    } else {
      await createClinicMutation.mutateAsync(payload);
    }
    dialogVisible.value = false;
    refresh();
  }

  function toggleStatus(clinic: ClinicResponseDto) {
    // ✅ 2. Use Enum for Comparison (No magic numbers)
    if (clinic.status === SubscriptionStatus.Active) {
      suspendMutation.mutate(clinic.id, { onSuccess: () => refresh() });
    } else {
      activateMutation.mutate(clinic.id, { onSuccess: () => refresh() });
    }
  }

  function restoreClinic(id: string) {
    restoreMutation.mutate(id, { onSuccess: () => refresh() });
  }

  function confirmDelete(clinic: ClinicResponseDto) {
    confirm.require({
      message: `Move ${clinic.name} to trash?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptClass: 'p-button-danger',
      accept: () => softDeleteMutation.mutate(clinic.id, { onSuccess: () => refresh() }),
    });
  }

  function confirmHardDelete(clinic: ClinicResponseDto) {
    confirm.require({
      message: `Permanently delete ${clinic.name}? This cannot be undone.`,
      header: 'Permanent Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptClass: 'p-button-danger',
      accept: () => hardDeleteMutation.mutate(clinic.id, { onSuccess: () => refresh() }),
    });
  }

  // ✅ Centralized Action Handler
  function handleTableAction(event: { type: string; clinic: ClinicResponseDto }) {
    const { type, clinic } = event;

    // Set context for whatever action we take
    selectedClinic.value = clinic;

    switch (type) {
      case 'edit':
        openEditDialog(clinic);
        break;

      // 💳 Subscription Management
      case 'manage-subscription':
        showSubscriptionDialog.value = true;
        break;

      // 🌳 Branch Management
      case 'manage-branches':
        showBranchDialog.value = true;
        break;

      case 'toggle-status':
        toggleStatus(clinic);
        break;
      case 'delete':
        confirmDelete(clinic);
        break;
      case 'restore':
        restoreClinic(clinic.id);
        break;
      case 'hard-delete':
        confirmHardDelete(clinic);
        break;
    }
  }
</script>
