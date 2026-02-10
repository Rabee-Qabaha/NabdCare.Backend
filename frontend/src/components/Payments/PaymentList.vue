<template>
  <div class="h-full flex flex-col">
    <DataTable
      :value="virtualPayments"
      class="text-md w-full flex-grow custom-table"
      striped-rows
      row-hover
      scrollable
      scroll-height="calc(100vh - 22rem)"
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

      <Column field="unallocatedAmount" header="Credit" sortable style="width: 15%">
        <template #loading><Skeleton width="4rem" height="1.5rem" /></template>
        <template #body="{ data }">
          <span
            v-if="data"
            :class="data.unallocatedAmount > 0 ? 'text-green-600 font-bold' : 'text-surface-500'"
          >
            {{ formatCurrency(data.unallocatedAmount) }}
          </span>
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
      :is-processing="isProcessing"
      :clinic-id="props.clinicId"
      @refresh="refresh"
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
  // PaymentList Component
  import PaymentDialog from '@/components/Payments/PaymentDialog.vue';
  import PaymentFilters from '@/components/Payments/PaymentFilters.vue';
  import { usePaymentActions } from '@/composables/query/payments/usePaymentActions.ts';
  import { usePaymentsTable } from '@/composables/query/payments/usePaymentsTable.ts';
  import { PaymentMethod } from '@/types/backend/payment-method';
  import { useConfirm } from 'primevue/useconfirm';
  import { computed, ref, watch } from 'vue';

  // PrimeVue Components
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import ConfirmDialog from 'primevue/confirmdialog';
  import DataTable from 'primevue/datatable';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  const props = defineProps<{
    clinicId: string;
    search?: string;
  }>();

  const emit = defineEmits(['update:total-records', 'update:loading']);

  const confirm = useConfirm();
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
  } = usePaymentsTable(computed(() => props.clinicId));

  // Sync loading state with parent
  watch(loading, (val) => {
    emit('update:loading', val);
  });

  // Sync search prop with filters
  watch(
    () => props.search,
    (newSearch) => {
      filters.value.global = newSearch || '';
    },
    { immediate: true },
  );

  watch(totalRecords, (val) => {
    emit('update:total-records', val);
  });

  const {
    createMutation,
    updateMutation,
    updateCheque,
    deleteMutation,
    canEdit,
    canDelete,
    isUpdatingCheque,
  } = usePaymentActions();

  const isProcessing = computed(
    () =>
      createMutation.isPending.value || updateMutation.isPending.value || isUpdatingCheque.value,
  );

  // --- Dialog State ---
  const dialogVisible = ref(false);

  // --- Handlers ---

  const openCreateDialog = () => {
    dialogVisible.value = true;
  };

  const closeDialog = () => {
    dialogVisible.value = false;
    dialogVisible.value = false;
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

  defineExpose({
    refresh,
    clearFilters,
    openCreateDialog,
    openFilterDialog: () => {
      showFilters.value = true;
    },
  });
</script>
