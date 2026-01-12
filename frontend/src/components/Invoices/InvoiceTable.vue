// Path: src/components/Invoices/InvoiceTable.vue
<script setup lang="ts">
  import { InvoiceStatus, type InvoiceDto } from '@/types/backend';
  import { formatCurrency, formatDate } from '@/utils/uiHelpers';
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Tag from 'primevue/tag';

  defineProps<{
    invoices: InvoiceDto[];
    loading: boolean;
    hasNextPage?: boolean;
    fetchingNext?: boolean;
  }>();

  const emit = defineEmits(['load-more', 'download', 'pay']);

  // UI Helpers
  const getStatusSeverity = (status: InvoiceStatus) => {
    switch (status) {
      case InvoiceStatus.Paid:
        return 'success';
      case InvoiceStatus.Issued:
        return 'warn';
      case InvoiceStatus.Overdue:
        return 'danger';
      case InvoiceStatus.Void:
        return 'secondary';
      default:
        return 'info';
    }
  };

  const getStatusLabel = (status: InvoiceStatus) => InvoiceStatus[status];

  const handleDownload = (invoice: InvoiceDto) => {
    if (invoice.pdfUrl) {
      window.open(invoice.pdfUrl, '_blank');
    } else {
      emit('download', invoice); // Fallback to API generation
    }
  };
</script>

<template>
  <div class="flex flex-col h-full">
    <DataTable
      :value="invoices"
      :loading="loading"
      size="small"
      striped-rows
      row-hover
      scrollable
      scroll-height="flex"
      class="text-sm flex-grow"
    >
      <template #empty>
        <div class="p-8 text-center text-surface-500">No invoices found.</div>
      </template>

      <Column header="Invoice #" style="min-width: 140px">
        <template #body="{ data }">
          <div class="flex flex-col">
            <span class="font-mono font-medium text-primary-600 dark:text-primary-400">
              {{ data.invoiceNumber }}
            </span>
            <span class="text-[10px] text-surface-500">
              {{ formatDate(data.issueDate) }}
            </span>
          </div>
        </template>
      </Column>

      <Column header="Amount">
        <template #body="{ data }">
          <span class="font-bold">{{ formatCurrency(data.totalAmount, data.currency) }}</span>
          <div v-if="data.balanceDue > 0" class="text-[10px] text-red-500">
            Due: {{ formatCurrency(data.balanceDue, data.currency) }}
          </div>
        </template>
      </Column>

      <Column header="Status">
        <template #body="{ data }">
          <Tag
            :value="getStatusLabel(data.status)"
            :severity="getStatusSeverity(data.status)"
            class="text-[10px] uppercase font-bold px-2"
            rounded
          />
        </template>
      </Column>

      <Column style="width: 100px" align-frozen="right" frozen>
        <template #body="{ data }">
          <div class="flex gap-1">
            <Button
              v-tooltip="'Download PDF'"
              icon="pi pi-download"
              text
              rounded
              size="small"
              severity="secondary"
              @click="handleDownload(data)"
            />

            <Button
              v-if="data.hostedPaymentUrl && data.status !== InvoiceStatus.Paid"
              v-tooltip="'Pay Now'"
              icon="pi pi-wallet"
              text
              rounded
              size="small"
              severity="success"
              @click="emit('pay', data)"
            />
          </div>
        </template>
      </Column>

      <template #footer>
        <div v-if="hasNextPage" class="flex justify-center p-2">
          <Button
            label="Load More"
            text
            size="small"
            :loading="fetchingNext"
            @click="emit('load-more')"
          />
        </div>
      </template>
    </DataTable>
  </div>
</template>
