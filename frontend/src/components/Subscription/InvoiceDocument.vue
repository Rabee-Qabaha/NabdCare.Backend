// src/components/Subscription/InvoiceDocument.vue
<script setup lang="ts">
  import type { InvoiceDto } from '@/types/backend';
  import { InvoiceStatus } from '@/types/backend';
  import { computed } from 'vue';

  // Components
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Divider from 'primevue/divider';
  import Tag from 'primevue/tag';

  export interface PaymentConfig {
    bankName?: string;
    accountName?: string;
    iban?: string;
    swift?: string;
    chequePayableTo?: string;
    acceptsCash?: boolean;
    cashLocation?: string;
  }

  const props = defineProps<{
    invoice: InvoiceDto;
    companyInfo: { name: string; address: string; email: string; phone?: string; website?: string };
    paymentConfig?: PaymentConfig;
  }>();

  const emit = defineEmits(['close']);

  const isPaid = computed(
    () => props.invoice.status === InvoiceStatus.Paid || props.invoice.balanceDue <= 0,
  );

  const handlePrint = () => window.print();

  const formatDate = (d: string | Date | undefined) => {
    if (!d) return '-';
    return new Date(d).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const formatMoney = (v: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(v);

  const getItemTypeLabel = (type: string | number) => {
    const t = String(type).toLowerCase();
    if (t.includes('base') || t === '0') return 'Base Plan';
    if (t.includes('user') || t === '1') return 'User Add-on';
    if (t.includes('branch') || t === '2') return 'Branch Add-on';
    return 'Service';
  };
</script>

<template>
  <Teleport to="body">
    <div id="invoice-print-view" class="invoice-overlay">
      <div class="print-controls fade-in-down">
        <div
          class="flex gap-2 bg-white/90 dark:bg-surface-800/90 backdrop-blur shadow-sm p-2 rounded-lg border border-surface-200 dark:border-surface-700"
        >
          <Button label="Print" icon="pi pi-print" severity="contrast" @click="handlePrint" />
          <Button
            label="Close"
            icon="pi pi-times"
            text
            severity="secondary"
            @click="emit('close')"
          />
        </div>
      </div>

      <div class="invoice-paper shadow-2xl relative overflow-hidden">
        <div
          v-if="isPaid"
          class="absolute top-10 right-10 border-4 border-green-600 text-green-600 font-black text-6xl opacity-20 transform -rotate-12 px-4 py-2 pointer-events-none select-none z-0"
        >
          PAID
        </div>

        <div class="flex justify-between items-start mb-10 relative z-10">
          <div class="flex flex-col gap-1">
            <div class="flex items-center gap-3 mb-2">
              <div
                class="w-10 h-10 bg-gray-900 text-white rounded-lg flex items-center justify-center"
              >
                <i class="pi pi-box text-xl"></i>
              </div>
              <span class="text-2xl font-bold tracking-tight text-gray-900">
                {{ companyInfo.name }}
              </span>
            </div>
            <p class="text-sm text-gray-500">{{ companyInfo.address }}</p>
            <p class="text-sm text-gray-500">{{ companyInfo.email }}</p>
          </div>

          <div class="text-right">
            <h1 class="text-3xl font-light text-gray-300 uppercase tracking-widest">Invoice</h1>
            <div class="mt-2">
              <Tag
                :value="isPaid ? 'PAID' : invoice.status"
                :severity="isPaid ? 'success' : 'warn'"
                class="px-3 py-1 text-xs font-bold uppercase"
              />
            </div>
          </div>
        </div>

        <div
          class="grid grid-cols-2 gap-12 mb-10 border-t border-b border-gray-100 py-8 relative z-10"
        >
          <div>
            <span class="text-xs font-bold text-gray-400 uppercase tracking-wider block mb-3">
              Bill To
            </span>
            <h2 class="text-lg font-bold text-gray-900 mb-1">{{ invoice.billedToName }}</h2>
            <p class="text-sm text-gray-600 whitespace-pre-line leading-relaxed">
              {{ invoice.billedToAddress || 'Address not provided' }}
            </p>
            <p v-if="invoice.billedToTaxNumber" class="text-sm text-gray-500 mt-2">
              Tax ID:
              <span class="font-mono text-gray-700">{{ invoice.billedToTaxNumber }}</span>
            </p>
          </div>

          <div class="flex flex-col gap-3 text-sm">
            <div class="flex justify-between">
              <span class="text-gray-500 font-medium">Invoice Number</span>
              <span class="font-bold text-gray-900 font-mono">{{ invoice.invoiceNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-gray-500 font-medium">Date Issued</span>
              <span class="text-gray-900">{{ formatDate(invoice.issueDate) }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-gray-500 font-medium">Due Date</span>
              <span
                class="font-bold"
                :class="{
                  'text-red-600': !isPaid && invoice.status === InvoiceStatus.Overdue,
                  'text-gray-900': isPaid || invoice.status !== InvoiceStatus.Overdue,
                }"
              >
                {{ formatDate(invoice.dueDate) }}
              </span>
            </div>
          </div>
        </div>

        <div class="mb-10 relative z-10">
          <DataTable :value="invoice.items" class="invoice-table" striped-rows>
            <Column header="Description">
              <template #body="{ data }">
                <div class="py-3">
                  <span class="font-bold text-gray-900 block text-sm">{{ data.description }}</span>
                  <span v-if="data.note" class="text-xs text-gray-500 block mt-1">
                    {{ data.note }}
                  </span>
                </div>
              </template>
            </Column>
            <Column header="Type" style="width: 1%; white-space: nowrap">
              <template #body="{ data }">
                <span
                  class="text-[10px] bg-gray-100 text-gray-600 border border-gray-200 px-2 py-1 rounded uppercase tracking-wide"
                >
                  {{ getItemTypeLabel(data.type) }}
                </span>
              </template>
            </Column>
            <Column
              header="Qty"
              field="quantity"
              class="text-center w-[10%] text-sm font-mono text-gray-600"
            />
            <Column header="Rate" class="text-right w-[15%] text-sm font-mono text-gray-600">
              <template #body="{ data }">{{ formatMoney(data.unitPrice) }}</template>
            </Column>
            <Column header="Amount" class="text-right w-[15%] text-sm">
              <template #body="{ data }">
                <span v-if="data.total > 0" class="font-bold font-mono text-gray-900">
                  {{ formatMoney(data.total) }}
                </span>
                <span
                  v-else
                  class="text-green-700 text-[10px] font-bold bg-green-50 px-2 py-1 rounded border border-green-100 uppercase"
                >
                  FREE
                </span>
              </template>
            </Column>
          </DataTable>
        </div>

        <div
          class="flex flex-col md:flex-row justify-between items-start gap-10 border-t border-gray-100 pt-8 relative z-10"
        >
          <div class="w-full md:w-1/2 text-sm text-gray-500">
            <div
              v-if="isPaid"
              class="bg-green-50 border border-green-100 text-green-700 p-4 rounded-lg flex items-center gap-3"
            >
              <i class="pi pi-check-circle text-xl"></i>
              <div>
                <p class="font-bold text-sm">Payment Received</p>
                <p class="text-xs opacity-80">This invoice has been settled. Thank you!</p>
              </div>
            </div>

            <div v-else-if="paymentConfig">
              <h4 class="font-bold text-gray-900 mb-3 uppercase text-xs tracking-wider">
                Payment Options
              </h4>
              <div class="space-y-4">
                <div
                  v-if="paymentConfig.iban"
                  class="bg-gray-50 p-3 rounded-lg border border-gray-100 text-xs space-y-1"
                >
                  <div class="font-bold text-gray-700 mb-1 flex items-center gap-2">
                    <i class="pi pi-building"></i>
                    Bank Transfer
                  </div>
                  <div class="flex justify-between">
                    <span class="text-gray-500">Bank:</span>
                    <span class="font-medium text-gray-900">{{ paymentConfig.bankName }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-gray-500">Acc:</span>
                    <span class="font-medium text-gray-900">{{ paymentConfig.accountName }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-gray-500">IBAN:</span>
                    <span class="font-medium text-gray-900 font-mono">
                      {{ paymentConfig.iban }}
                    </span>
                  </div>
                </div>

                <div
                  v-if="paymentConfig.acceptsCash"
                  class="bg-gray-50 p-3 rounded-lg border border-gray-100 text-xs"
                >
                  <div class="font-bold text-gray-700 mb-1 flex items-center gap-2">
                    <i class="pi pi-money-bill"></i>
                    Cash
                  </div>
                  <p>
                    Accepted at:
                    <span class="text-gray-900">
                      {{ paymentConfig.cashLocation || 'Main Office' }}
                    </span>
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div class="w-full md:w-5/12 space-y-3">
            <div class="flex justify-between text-sm text-gray-600">
              <span>Subtotal</span>
              <span class="font-medium font-mono">{{ formatMoney(invoice.subTotal) }}</span>
            </div>
            <div class="flex justify-between text-sm text-gray-600">
              <span>Tax (0%)</span>
              <span class="font-medium font-mono">{{ formatMoney(invoice.taxAmount) }}</span>
            </div>
            <div
              v-if="invoice.paidAmount > 0"
              class="flex justify-between text-sm text-green-700 bg-green-50 px-3 py-1.5 rounded"
            >
              <span class="font-medium">Paid</span>
              <span class="font-bold font-mono">- {{ formatMoney(invoice.paidAmount) }}</span>
            </div>
            <Divider class="my-2 border-gray-200" />
            <div class="flex justify-between items-center">
              <span class="text-base font-bold text-gray-900 uppercase tracking-wide">
                Total Due
              </span>
              <span class="text-3xl font-bold text-gray-900 font-mono tracking-tight">
                {{ formatMoney(invoice.balanceDue) }}
              </span>
            </div>
          </div>
        </div>

        <div class="mt-auto pt-16 text-center">
          <p class="text-xs text-gray-400 font-medium uppercase tracking-widest">
            Thank you for your business
          </p>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style>
  @media print {
    @page {
      size: A4;
      margin: 0;
    }

    /* 1. Hide everything NOT the invoice */
    body > *:not(#invoice-print-view) {
      display: none !important;
    }

    /* 2. Position the invoice */
    #invoice-print-view,
    .invoice-paper {
      visibility: visible !important;
      background: white !important;
      color: black !important;
      display: block !important;
      position: absolute !important;
      left: 0 !important;
      top: 0 !important;
      width: 100% !important;
      height: 100% !important;
      margin: 0 !important;
      padding: 15mm !important;
      box-shadow: none !important;
      border: none !important;
      z-index: 99999;
    }

    /* 3. Hide Controls */
    .print-controls {
      display: none !important;
    }
  }
</style>

<style scoped>
  .invoice-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    background: rgba(0, 0, 0, 0.6);
    backdrop-filter: blur(4px);
    z-index: 9999;
    overflow-y: auto;
    padding: 2rem;
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  .invoice-paper {
    background: white; /* Always White Paper */
    color: #111827; /* Always Dark Text (Tailwind gray-900) */
    width: 210mm;
    min-height: 297mm;
    padding: 15mm 20mm;
    position: relative;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
  }

  .print-controls {
    position: sticky;
    top: 1rem;
    z-index: 50;
    margin-bottom: 1.5rem;
  }

  /* * 🛑 FORCE OVERRIDES FOR DATATABLE 
   * These rules ensure the table looks like it's printed on white paper
   * even if the app is in Dark Mode.
   */

  /* 1. Reset Header Background & Text */
  :deep(.invoice-table .p-datatable-thead > tr > th) {
    background-color: #f8fafc !important; /* Light Gray bg */
    color: #64748b !important; /* Slate-500 text */
    border-bottom: 2px solid #e2e8f0 !important;
    text-transform: uppercase;
    font-size: 0.7rem;
    font-weight: 700;
    padding: 0.75rem 1rem;
  }

  /* 2. Reset Body Background & Text */
  :deep(.invoice-table .p-datatable-tbody > tr) {
    background-color: transparent !important;
    color: #111827 !important; /* Force Black Text */
  }

  :deep(.invoice-table .p-datatable-tbody > tr > td) {
    background-color: transparent !important;
    color: #111827 !important; /* Force Black Text on Cells */
    border-bottom: 1px solid #f1f5f9 !important;
    padding: 0.75rem 1rem;
    vertical-align: top;
  }

  /* 3. Ensure "Striped" rows are subtle gray, not dark */
  :deep(.invoice-table.p-datatable-striped .p-datatable-tbody > tr:nth-child(even)) {
    background-color: #f9fafb !important; /* Very light gray */
  }

  /* 4. Fix specific span colors inside the table (like your example) */
  :deep(.invoice-table span.text-gray-900),
  :deep(.invoice-table div.text-gray-900) {
    color: #111827 !important;
  }

  :deep(.invoice-table span.text-gray-600),
  :deep(.invoice-table div.text-gray-600) {
    color: #4b5563 !important;
  }

  /* 5. Force Badges/Tags to Light Mode look */
  :deep(.invoice-table .bg-gray-100) {
    background-color: #f3f4f6 !important;
    color: #4b5563 !important;
    border-color: #e5e7eb !important;
  }

  /* Remove bottom border on last row */
  :deep(.invoice-table .p-datatable-tbody > tr:last-child > td) {
    border-bottom: none !important;
  }
</style>
