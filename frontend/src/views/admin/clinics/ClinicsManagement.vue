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

      <SubscriptionManagerDialog
        v-if="showSubscriptionDialog"
        v-model:visible="showSubscriptionDialog"
        :clinic-id="targetClinicId"
        :clinic-name="targetClinicName"
        @saved="refresh"
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
  import { SubscriptionStatus, type ClinicResponseDto } from '@/types/backend';
  import { useConfirm } from 'primevue/useconfirm';
  import { useToast } from 'primevue/usetoast';
  import { ref } from 'vue';

  // Components
  import BranchManagerDialog from '@/components/Branches/BranchManagerDialog.vue';
  import ClinicDialog from '@/components/Clinic/ClinicDialog.vue';
  import ClinicFilters from '@/components/Clinic/ClinicFilters.vue';
  import ClinicTable from '@/components/Clinic/ClinicTable.vue';
  import ClinicToolbar from '@/components/Clinic/ClinicToolbar.vue';
  import SubscriptionManagerDialog from '@/components/Subscription/SubscriptionManagerDialog.vue';
  import PageHeader from '@/layout/PageHeader.vue';

  import ConfirmDialog from 'primevue/confirmdialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';

  // State
  const includeDeleted = ref(false);
  const dialogVisible = ref(false);
  const showFilters = ref(false);
  const showSubscriptionDialog = ref(false);
  const showBranchDialog = ref(false);

  const selectedClinic = ref<ClinicResponseDto | null>(null);

  // ✅ New Refs for Subscription context (Decouples Wizard flow from Table selection)
  const targetClinicId = ref<string | null>(null);
  const targetClinicName = ref<string>('');

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
  const toast = useToast();

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

  // ✅ THE WIZARD LOGIC
  async function handleSave(payload: any) {
    try {
      if (payload.id) {
        // --- EDIT MODE ---
        await updateClinicMutation.mutateAsync({ id: payload.id, data: payload });
        dialogVisible.value = false;
        refresh();
      } else {
        // --- CREATE MODE (Wizard Step 1) ---
        // 1. Create Identity
        const newClinic = await createClinicMutation.mutateAsync(payload);

        // 2. Close Step 1
        dialogVisible.value = false;

        // 3. Prep Step 2
        targetClinicId.value = newClinic.id;
        targetClinicName.value = newClinic.name;

        // 4. Open Step 2 (Subscription)
        showSubscriptionDialog.value = true;

        // 5. Notify & Refresh Background
        toast.add({
          severity: 'success',
          summary: 'Identity Created',
          detail: 'Please configure the subscription plan now.',
          life: 5000,
        });
        refresh();
      }
    } catch (error) {
      console.error('Save failed', error);
      // Errors handled globally by mutation, but catch here to stop flow
    }
  }

  function toggleStatus(clinic: ClinicResponseDto) {
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

  // Centralized Action Handler
  function handleTableAction(event: { type: string; clinic: ClinicResponseDto }) {
    const { type, clinic } = event;

    // Set context
    selectedClinic.value = clinic;

    switch (type) {
      case 'edit':
        openEditDialog(clinic);
        break;

      case 'manage-subscription':
        // ✅ Populate target refs for existing clinics
        targetClinicId.value = clinic.id;
        targetClinicName.value = clinic.name;
        showSubscriptionDialog.value = true;
        break;

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
