<template>
  <Dialog
    :visible="visible"
    modal
    header="Payment Details"
    :style="{ width: '50rem' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }"
    :dismissable-mask="true"
    @update:visible="$emit('update:visible', $event)"
  >
    <div v-if="payment" class="flex flex-col gap-6">
      <!-- Header Stats -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div
          class="bg-surface-50 dark:bg-surface-800 p-4 rounded-xl border border-surface-100 dark:border-surface-700"
        >
          <span class="text-sm text-surface-500 block mb-1">Amount</span>
          <span class="text-xl font-bold text-primary-600">
            {{ formatCurrency(payment.amount, payment.currency) }}
          </span>
        </div>
        <div
          class="bg-surface-50 dark:bg-surface-800 p-4 rounded-xl border border-surface-100 dark:border-surface-700"
        >
          <span class="text-sm text-surface-500 block mb-1">Date</span>
          <span class="font-bold text-surface-900 dark:text-surface-0">
            {{ formatDate(payment.paymentDate) }}
          </span>
        </div>
        <div
          class="bg-surface-50 dark:bg-surface-800 p-4 rounded-xl border border-surface-100 dark:border-surface-700"
        >
          <span class="text-sm text-surface-500 block mb-1">Status</span>
          <Tag :value="payment.status" :severity="getStatusSeverity(payment.status)" />
        </div>
      </div>

      <!-- Exchange Rate Section (Conditional) -->
      <div
        v-if="payment.currency !== functionalCurrency || payment.finalExchangeRate"
        class="border rounded-xl p-4 bg-amber-50 dark:bg-amber-900/10 border-amber-200 dark:border-amber-800"
      >
        <h3
          class="text-sm font-bold text-amber-800 dark:text-amber-200 uppercase mb-3 flex items-center gap-2"
        >
          <i class="pi pi-sync"></i>
          Currency Exchange
        </h3>
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div>
            <span class="text-xs text-amber-700/70 dark:text-amber-300/70 block">Base Rate</span>
            <span class="font-mono font-bold text-amber-900 dark:text-amber-100">
              {{ payment.baseExchangeRate || '-' }}
            </span>
          </div>
          <div>
            <span class="text-xs text-amber-700/70 dark:text-amber-300/70 block">Final Rate</span>
            <span class="font-mono font-bold text-amber-900 dark:text-amber-100">
              {{ payment.finalExchangeRate || '-' }}
            </span>
          </div>
          <div>
            <span class="text-xs text-amber-700/70 dark:text-amber-300/70 block">
              Converted Amount
            </span>
            <span class="font-bold text-amber-900 dark:text-amber-100">
              {{ formatCurrency(payment.amountInFunctionalCurrency, functionalCurrency) }}
            </span>
          </div>
          <div v-if="payment.unallocatedAmount > 0">
            <span class="text-xs text-amber-700/70 dark:text-amber-300/70 block">
              Unallocated Credit
            </span>
            <span class="font-bold text-green-600">
              {{ formatCurrency(payment.unallocatedAmount, functionalCurrency) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Method Details -->
      <div class="border-t border-surface-100 dark:border-surface-700 pt-4">
        <h3 class="text-sm font-bold text-surface-900 dark:text-surface-0 uppercase mb-3">
          Payment Method: {{ payment.method }}
        </h3>

        <!-- Cheque Details -->
        <div
          v-if="payment.method === 'Cheque' && payment.chequeDetail"
          class="bg-white dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-4"
        >
          <div class="grid grid-cols-1 md:grid-cols-2 gap-y-4 gap-x-8">
            <div
              class="flex justify-between border-b border-surface-100 dark:border-surface-800 pb-2"
            >
              <span class="text-surface-500">Cheque Number</span>
              <span class="font-mono font-medium">{{ payment.chequeDetail.chequeNumber }}</span>
            </div>
            <div
              class="flex justify-between border-b border-surface-100 dark:border-surface-800 pb-2"
            >
              <span class="text-surface-500">Bank Name</span>
              <span class="font-medium">{{ payment.chequeDetail.bankName }}</span>
            </div>
            <div
              class="flex justify-between border-b border-surface-100 dark:border-surface-800 pb-2"
            >
              <span class="text-surface-500">Branch</span>
              <span class="font-medium">{{ payment.chequeDetail.branch || '-' }}</span>
            </div>
            <div
              class="flex justify-between border-b border-surface-100 dark:border-surface-800 pb-2"
            >
              <span class="text-surface-500">Due Date</span>
              <span class="font-medium">{{ formatDate(payment.chequeDetail.dueDate) }}</span>
            </div>
            <div class="flex justify-between items-center">
              <span class="text-surface-500">Amount</span>
              <div class="flex flex-col items-end">
                <span class="font-bold">
                  {{ formatCurrency(payment.chequeDetail.amount, payment.chequeDetail.currency) }}
                </span>
                <span
                  v-if="payment.chequeDetail.currency !== payment.currency"
                  class="text-xs text-surface-400"
                >
                  (Differs from payment currency)
                </span>
              </div>
            </div>
            <div v-if="payment.chequeDetail.imageUrl" class="flex justify-between items-center">
              <span class="text-surface-500">Image</span>
              <a
                :href="payment.chequeDetail.imageUrl"
                target="_blank"
                class="text-primary-600 hover:underline flex items-center gap-1"
              >
                <i class="pi pi-image"></i>
                View Cheque
              </a>
            </div>
          </div>
        </div>

        <!-- Reference / Transaction ID -->
        <div v-if="payment.transactionId" class="mt-4 flex gap-2">
          <span class="text-surface-500">Transaction ID:</span>
          <span class="font-mono bg-surface-100 dark:bg-surface-800 px-2 rounded text-sm">
            {{ payment.transactionId }}
          </span>
        </div>
      </div>

      <!-- Notes -->
      <div
        v-if="payment.notes"
        class="bg-surface-50 dark:bg-surface-800 p-4 rounded-lg text-sm text-surface-600 dark:text-surface-300 italic"
      >
        <i class="pi pi-comment mr-2 accent-primary-500"></i>
        "{{ payment.notes }}"
      </div>
    </div>

    <template #footer>
      <Button label="Close" icon="pi pi-times" text @click="$emit('update:visible', false)" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { useClinicQueries } from '@/composables/query/clinics/useClinicQueries';
  import type { PaymentDto } from '@/types/backend/payment-dto';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  const props = defineProps<{
    visible: boolean;
    payment: PaymentDto | null;
    clinicId?: string;
  }>();

  defineEmits(['update:visible']);

  import { useConfiguration } from '@/composables/useConfiguration';

  const { useClinicDetails } = useClinicQueries();
  const { functionalCurrency: systemCurrency } = useConfiguration();

  const clinicIdRef = computed(() => props.clinicId || '');
  const { data: clinic } = useClinicDetails(clinicIdRef);
  const functionalCurrency = computed(
    () => clinic.value?.settings?.currency || systemCurrency.value || 'USD',
  );

  const formatCurrency = (val: number, currency?: string) => {
    const code = currency || functionalCurrency.value;
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: code }).format(val);
  };

  const formatDate = (dateInput: string | Date) => {
    if (!dateInput) return '-';
    const date = new Date(dateInput);
    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
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
