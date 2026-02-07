<template>
  <div class="card p-0 border-0 shadow-none h-full">
    <!-- Header / Toolbar -->
    <div
      class="flex flex-col gap-3 rounded-xl border border-surface-200 bg-white p-3 shadow-sm dark:border-surface-700 dark:bg-surface-900 md:flex-row md:items-center md:justify-between mb-4"
    >
      <div class="flex flex-wrap gap-2">
        <Button
          v-if="canCreate"
          label="Add Payment"
          icon="pi pi-plus"
          class="w-full font-semibold shadow-sm sm:w-auto"
          @click="openCreateDialog"
        />
      </div>

      <div class="flex flex-wrap items-center gap-2">
        <div class="flex items-center gap-2 mr-2">
          <span class="text-surface-500 font-medium text-sm">Total:</span>
          <Tag :value="totalRecords" severity="secondary" rounded />
        </div>

        <!-- Filter Button -->
        <Button
          label="Filters"
          icon="pi pi-sliders-h"
          severity="secondary"
          outlined
          class="w-full sm:w-auto"
          @click="showFilters = true"
        />

        <div class="mx-1 hidden h-6 w-px bg-surface-200 dark:bg-surface-700 md:block"></div>

        <div class="flex w-full justify-end gap-1 sm:w-auto">
          <Button
            v-tooltip.top="'Reset filters'"
            icon="pi pi-filter-slash"
            severity="secondary"
            text
            rounded
            @click="clearFilters"
          />

          <Button
            v-tooltip.top="'Refresh'"
            icon="pi pi-refresh"
            severity="secondary"
            text
            rounded
            :loading="loading"
            @click="refresh"
          />
        </div>
      </div>
    </div>

    <!-- Search Bar -->
    <div class="mb-4">
      <IconField class="w-full">
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="filters.global"
          placeholder="Search payments by transaction ID, method or status..."
          class="w-full"
        />
      </IconField>
    </div>

    <!-- Payments Table (Virtual Scroll) -->
    <DataTable
      :value="virtualPayments"
      scrollable
      scroll-height="calc(100vh - 21rem)"
      :virtual-scroller-options="{
        lazy: true,
        onLazyLoad: loadPaymentsLazy,
        itemSize: 60,
        delay: 200,
        showLoader: true,
        loading: loading,
        numToleratedItems: 10,
      }"
      data-key="id"
      table-style="min-width: 50rem"
      @sort="onSort"
    >
      <template #empty>
        <div class="text-center p-8">
          <i class="pi pi-folder-open text-4xl text-surface-400 mb-2"></i>
          <p class="text-surface-500">No payments found.</p>
        </div>
      </template>

      <Column field="paymentDate" header="Date" sortable style="width: 15%">
        <template #loading><Skeleton width="100%" height="1.5rem" /></template>
        <template #body="{ data }">
          <span v-if="data">{{ formatDate(data.paymentDate) }}</span>
          <Skeleton v-else width="100%" height="1.5rem" />
        </template>
      </Column>

      <Column field="amount" header="Amount" sortable style="width: 15%">
        <template #loading><Skeleton width="4rem" height="1.5rem" /></template>
        <template #body="{ data }">
          <span v-if="data" class="font-bold">{{ formatCurrency(data.amount) }}</span>
          <Skeleton v-else width="4rem" height="1.5rem" />
        </template>
      </Column>

      <Column field="method" header="Method" style="width: 15%">
        <template #loading><Skeleton width="6rem" height="1.5rem" /></template>
        <template #body="{ data }">
          <div v-if="data" class="flex items-center gap-2">
            <i :class="getMethodIcon(data.method)" class="text-surface-500"></i>
            <span>{{ data.method }}</span>
          </div>
          <Skeleton v-else width="6rem" height="1.5rem" />
        </template>
      </Column>

      <Column header="Reference" style="width: 25%">
        <template #loading><Skeleton width="8rem" height="1.5rem" /></template>
        <template #body="{ data }">
          <template v-if="data">
            <div v-if="data.method === 'Cheque' && data.chequeDetail" class="text-sm">
              Checks#: {{ data.chequeDetail.chequeNumber }}
              <br />
              <span class="text-xs text-surface-500">{{ data.chequeDetail.bankName }}</span>
            </div>
            <div v-else-if="data.transactionId" class="text-sm font-mono">
              {{ data.transactionId }}
            </div>
            <div v-else class="text-surface-400 italic text-sm">-</div>
          </template>
          <Skeleton v-else width="8rem" height="1.5rem" />
        </template>
      </Column>

      <Column field="status" header="Status" style="width: 15%">
        <template #loading><Skeleton width="5rem" height="1.5rem" /></template>
        <template #body="{ data }">
          <Tag v-if="data" :value="data.status" :severity="getStatusSeverity(data.status)" />
          <Skeleton v-else width="5rem" height="1.5rem" />
        </template>
      </Column>

      <Column header="Actions" :exportable="false" style="min-width: 8rem; width: 15%">
        <template #loading><Skeleton shape="circle" size="2rem" /></template>
        <template #body="{ data }">
          <div v-if="data" class="flex gap-2">
            <Button
              v-if="canEdit"
              icon="pi pi-pencil"
              text
              rounded
              severity="secondary"
              @click="openEditDialog(data)"
              aria-label="Edit"
            />
            <Button
              v-if="canDelete"
              icon="pi pi-trash"
              text
              rounded
              severity="danger"
              @click="confirmDelete(data)"
              aria-label="Delete"
            />
          </div>
          <Skeleton v-else shape="circle" size="2rem" />
        </template>
      </Column>
    </DataTable>

    <!-- Dialog -->
    <PaymentDialog
      v-model:visible="dialogVisible"
      :payment="selectedPayment"
      :is-processing="isProcessing"
      :clinic-id="clinicId"
      @save="handleSave"
      @cancel="closeDialog"
    />

    <!-- Filter Drawer -->
    <PaymentFilters
      v-model:visible="showFilters"
      :filters="filters"
      @apply="applyFilters"
      @reset="clearFilters"
    />

    <ConfirmDialog />
  </div>
</template>

<script setup lang="ts">
  import PaymentDialog from '@/components/Payments/PaymentDialog.vue';
  import PaymentFilters from '@/components/Payments/PaymentFilters.vue';
  import { usePaymentActions } from '@/composables/query/payments/usePaymentActions.ts';
  import { usePaymentsTable } from '@/composables/query/payments/usePaymentsTable.ts'; // Updated import
  import { PaymentMethod } from '@/types/backend/payment-method';
  import { useConfirm } from 'primevue/useconfirm';
  import { computed, ref } from 'vue';
  import { useRoute } from 'vue-router';

  // PrimeVue Components
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import ConfirmDialog from 'primevue/confirmdialog';
  import DataTable from 'primevue/datatable';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  const route = useRoute();
  const confirm = useConfirm();
  const clinicId = computed(() => route.params.id as string);
  const showFilters = ref(false);

  // --- Data Fetching (Virtual Scroll) ---
  const {
    virtualPayments,
    totalRecords,
    loading,
    filters,
    loadPaymentsLazy,
    onSort,
    refresh,
    clearFilters,
    applyFilters,
  } = usePaymentsTable(clinicId);

  const { createMutation, updateMutation, deleteMutation, canCreate, canEdit, canDelete } =
    usePaymentActions();

  const isProcessing = computed(
    () => createMutation.isPending.value || updateMutation.isPending.value,
  );

  // --- Dialog State ---
  const dialogVisible = ref(false);
  const selectedPayment = ref<any>(null);

  // --- Handlers ---

  const openCreateDialog = () => {
    selectedPayment.value = {};
    dialogVisible.value = true;
  };

  const openEditDialog = (payment: any) => {
    selectedPayment.value = { ...payment };
    dialogVisible.value = true;
  };

  const closeDialog = () => {
    dialogVisible.value = false;
    selectedPayment.value = null;
  };

  const handleSave = (paymentData: any) => {
    const onSuccess = () => {
      closeDialog();
      refresh(); // Refresh list to show new/updated item
    };

    if (paymentData.id) {
      updateMutation.mutate({ id: paymentData.id, dto: paymentData }, { onSuccess });
    } else {
      createMutation.mutate(paymentData, { onSuccess });
    }
  };

  const confirmDelete = (payment: any) => {
    confirm.require({
      message: 'Are you sure you want to delete this payment record?',
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      acceptClass: 'p-button-danger',
      accept: () => {
        deleteMutation.mutate(payment.id, {
          onSuccess: () => refresh(),
        });
      },
    });
  };

  // --- Formatters ---
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'ILS' }).format(value);
  };

  const formatDate = (dateInput: string | Date) => {
    if (!dateInput) return '-';
    const date = new Date(dateInput);
    return date.toLocaleDateString('en-GB'); // dd/mm/yyyy
  };

  const getMethodIcon = (method: string) => {
    switch (method) {
      case PaymentMethod.Cash:
        return 'pi pi-money-bill';
      case PaymentMethod.Cheque:
        return 'pi pi-ticket';
      case PaymentMethod.Visa:
        return 'pi pi-credit-card';
      case PaymentMethod.BankTransfer:
        return 'pi pi-building';
      default:
        return 'pi pi-wallet';
    }
  };

  const getStatusSeverity = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'success';
      case 'pending':
        return 'warning';
      case 'failed':
      case 'bounced':
        return 'danger';
      case 'cancelled':
      case 'refunded':
        return 'secondary';
      default:
        return 'secondary';
    }
  };
</script>
