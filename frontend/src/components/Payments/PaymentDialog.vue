<template>
  <Dialog
    :visible="visible"
    :header="isEdit ? 'Edit Payment' : 'Add Payment'"
    :modal="true"
    :style="{ width: '600px', maxWidth: '100%' }"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="flex flex-col gap-6">
      <div
        v-if="props.invoiceId"
        class="bg-blue-50 dark:bg-blue-900/20 p-3 rounded-lg flex items-center gap-3 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800"
      >
        <i class="pi pi-file text-xl"></i>
        <div>
          <span class="font-bold block">Paying for Invoice</span>
          <span class="text-sm opacity-80">
            Payment will be allocated to this invoice automatically.
          </span>
        </div>
      </div>

      <!-- Method and Date Row -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label for="method" class="mb-2 block font-bold">Payment Method</label>
          <Select
            id="method"
            v-model="localPayment.method"
            :options="paymentMethodOptions"
            option-label="label"
            option-value="value"
            placeholder="Select Method"
            :invalid="submitted && !localPayment.method"
            class="w-full"
          />
          <small v-if="submitted && !localPayment.method" class="text-red-500">Required.</small>
        </div>

        <div>
          <label for="paymentDate" class="mb-2 block font-bold">Payment Date</label>
          <DatePicker
            id="paymentDate"
            v-model="localPayment.paymentDate"
            :show-icon="true"
            :show-button-bar="true"
            date-format="dd/mm/yy"
            placeholder="dd/mm/yyyy"
            :invalid="submitted && !localPayment.paymentDate"
            class="w-full"
          />
          <small v-if="submitted && !localPayment.paymentDate" class="text-red-500">
            Required.
          </small>
        </div>
      </div>

      <!-- Amount and Transaction ID Row -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label for="amount" class="mb-2 block font-bold">Amount</label>
          <InputNumber
            id="amount"
            v-model="localPayment.amount"
            mode="currency"
            currency="ILS"
            locale="en-US"
            :min="0"
            :max-fraction-digits="2"
            :invalid="submitted && !isAmountValid"
            class="w-full"
          />
          <small v-if="submitted && !isAmountValid" class="text-red-500">Must be positive.</small>
        </div>

        <div
          v-if="
            localPayment.method !== PaymentMethod.Cash &&
            localPayment.method !== PaymentMethod.Cheque
          "
        >
          <label for="transactionId" class="mb-2 block font-bold">Transaction ID</label>
          <InputText
            id="transactionId"
            v-model="localPayment.transactionId"
            class="w-full"
            placeholder="e.g. Ref-12345"
          />
        </div>
      </div>

      <!-- Cheque Details Panel -->
      <div
        v-if="localPayment.method === PaymentMethod.Cheque"
        class="p-4 border rounded-lg bg-surface-50 dark:bg-surface-900 border-surface-200 dark:border-surface-700 space-y-4"
      >
        <h3 class="font-bold text-lg mb-2 flex items-center gap-2">
          <i class="pi pi-briefcase text-primary-500"></i>
          Cheque Details
        </h3>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label for="bankName" class="mb-1 block text-sm font-medium">Bank Name</label>
            <InputText
              id="bankName"
              v-model="chequeDetail.bankName"
              class="w-full p-inputtext-sm"
              :invalid="submitted && !chequeDetail.bankName"
              placeholder="e.g. Bank Hapoalim"
            />
            <small v-if="submitted && !chequeDetail.bankName" class="text-red-500">Required</small>
          </div>
          <div>
            <label for="branch" class="mb-1 block text-sm font-medium">Branch</label>
            <InputText
              id="branch"
              v-model="chequeDetail.branch"
              class="w-full p-inputtext-sm"
              placeholder="e.g. 123"
            />
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label for="chequeNumber" class="mb-1 block text-sm font-medium">Cheque Number</label>
            <InputText
              id="chequeNumber"
              v-model="chequeDetail.chequeNumber"
              class="w-full p-inputtext-sm"
              :invalid="submitted && !chequeDetail.chequeNumber"
              placeholder="e.g. 000123"
            />
            <small v-if="submitted && !chequeDetail.chequeNumber" class="text-red-500">
              Required
            </small>
          </div>
          <div>
            <label for="dueDate" class="mb-1 block text-sm font-medium">Due Date</label>
            <DatePicker
              id="dueDate"
              v-model="chequeDetail.dueDate"
              class="w-full p-inputtext-sm"
              :show-icon="true"
              date-format="dd/mm/yy"
            />
          </div>
        </div>

        <div>
          <label for="issueDate" class="mb-1 block text-sm font-medium">Issue Date</label>
          <DatePicker
            id="issueDate"
            v-model="chequeDetail.issueDate"
            class="w-full p-inputtext-sm"
            :show-icon="true"
            date-format="dd/mm/yy"
          />
        </div>

        <div>
          <label for="imageUrl" class="mb-1 block text-sm font-medium">Cheque Image</label>
          <!-- Placeholder for Image Upload - using a textual representation for now as backend requires URL string -->
          <InputText
            id="imageUrl"
            v-model="chequeDetail.imageUrl"
            class="w-full p-inputtext-sm"
            placeholder="Image URL (Upload not implemented)"
          />
        </div>
      </div>

      <!-- Notes -->
      <div>
        <label for="notes" class="mb-2 block font-bold">Notes</label>
        <Textarea
          id="notes"
          v-model="localPayment.notes"
          rows="3"
          class="w-full"
          placeholder="Optional notes..."
        />
      </div>
    </div>

    <template #footer>
      <Button label="Cancel" icon="pi pi-times" text severity="secondary" @click="onCancel" />
      <Button
        label="Save Payment"
        icon="pi pi-check"
        :loading="props.isProcessing"
        @click="onSave"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { CreateChequeDetailDto } from '@/types/backend/create-cheque-detail-dto';
  import type { CreatePaymentRequestDto } from '@/types/backend/create-payment-request-dto';
  import { PaymentContext } from '@/types/backend/payment-context';
  import { PaymentMethod } from '@/types/backend/payment-method';
  import { computed, ref, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    payment?: Partial<CreatePaymentRequestDto> & { id?: string };
    isProcessing: boolean;
    clinicId: string;
    invoiceId?: string;
    maxAmount?: number;
  }>();

  const emit = defineEmits<{
    (e: 'update:visible', visible: boolean): void;
    (e: 'save', payment: CreatePaymentRequestDto & { id?: string }): void;
    (e: 'cancel'): void;
  }>();

  const localPayment = ref<Partial<CreatePaymentRequestDto> & { id?: string }>({
    amount: 0,
    method: PaymentMethod.Cash,
    paymentDate: new Date(),
    context: PaymentContext.Clinic,
    allocations: [],
  });

  const chequeDetail = ref<CreateChequeDetailDto>(new CreateChequeDetailDto());
  const submitted = ref(false);
  const isEdit = computed(() => !!props.payment?.id);

  const paymentMethodOptions = Object.values(PaymentMethod).map((method) => ({
    label: method,
    value: method,
  }));

  watch(
    () => props.visible,
    (newVal) => {
      if (newVal) {
        submitted.value = false;
        // Initialize
        localPayment.value = {
          amount: props.maxAmount || 0,
          method: PaymentMethod.Cash,
          paymentDate: new Date(),
          context: PaymentContext.Clinic,
          clinicId: props.clinicId,
          allocations: [],
          ...props.payment,
        };

        // Handle dates conversion if string
        if (typeof localPayment.value.paymentDate === 'string') {
          localPayment.value.paymentDate = new Date(localPayment.value.paymentDate);
        }

        // Initialize cheque detail
        if (localPayment.value.chequeDetail) {
          chequeDetail.value = { ...localPayment.value.chequeDetail };
          if (typeof chequeDetail.value.dueDate === 'string')
            chequeDetail.value.dueDate = new Date(chequeDetail.value.dueDate);
          if (typeof chequeDetail.value.issueDate === 'string')
            chequeDetail.value.issueDate = new Date(chequeDetail.value.issueDate);
        } else {
          chequeDetail.value = new CreateChequeDetailDto();
          chequeDetail.value.issueDate = new Date();
          chequeDetail.value.dueDate = new Date();
          // Default bank/details empty
          chequeDetail.value.chequeNumber = '';
          chequeDetail.value.bankName = '';
        }
      }
    },
  );

  // Validation
  const isAmountValid = computed(
    () => typeof localPayment.value.amount === 'number' && localPayment.value.amount > 0,
  );

  const isFormValid = computed(() => {
    let valid =
      isAmountValid.value && !!localPayment.value.method && !!localPayment.value.paymentDate;
    if (localPayment.value.method === PaymentMethod.Cheque) {
      valid = valid && !!chequeDetail.value.chequeNumber && !!chequeDetail.value.bankName;
    }
    return valid;
  });

  function onSave() {
    submitted.value = true;
    if (!isFormValid.value) return;

    // Construct Payload
    const payload: CreatePaymentRequestDto & { id?: string } = {
      ...localPayment.value,
      // Ensure strictly typed fields
      amount: localPayment.value.amount!,
      method: localPayment.value.method!,
      paymentDate: localPayment.value.paymentDate!,
      context: localPayment.value.context || PaymentContext.Clinic,
      clinicId: props.clinicId,
      allocations: props.invoiceId
        ? [{ invoiceId: props.invoiceId, amount: localPayment.value.amount! }]
        : [],
      chequeDetail:
        localPayment.value.method === PaymentMethod.Cheque ? chequeDetail.value : undefined,
    } as any;

    emit('save', payload);
  }

  function onCancel() {
    emit('cancel');
    emit('update:visible', false);
  }
</script>
