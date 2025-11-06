<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :header="localPayment.id ? 'Edit Payment' : 'Add Payment'"
    :modal="true"
    :style="{ width: '400px', maxWidth: '100%' }"
  >
    <div class="flex flex-col gap-6">
      <!-- Amount -->
      <div>
        <label for="amount" class="mb-2 block font-bold">Amount</label>
        <InputNumber
          id="amount"
          v-model="localPayment.amount"
          mode="currency"
          currency="ILS"
          locale="en-US"
          :step="0.01"
          :minFractionDigits="0"
          :maxFractionDigits="2"
          aria-label="Payment Amount"
          :invalid="submitted && !isAmountValid"
          class="w-full"
        />
        <small v-if="submitted && !isAmountValid" class="text-red-500"
          >Amount must be a positive number.</small
        >
      </div>

      <!-- Method -->
      <!-- Replace [] with paymentOptions when available -->
      <div>
        <label for="method" class="mb-2 block font-bold">Payment Method</label>
        <Select
          id="method"
          v-model="localPayment.method"
          :options="[]"
          optionLabel="label"
          optionValue="value"
          placeholder="Select Payment Method"
          :invalid="submitted && !localPayment.method"
          class="w-full"
        />
        <small v-if="submitted && !localPayment.method" class="text-red-500"
          >Payment method is required.</small
        >
      </div>

      <!-- Payment Date -->
      <div>
        <label for="paidAt" class="mb-2 block font-bold">Payment Date</label>
        <DatePicker
          id="paidAt"
          v-model="localPayment.paidAt"
          :showIcon="true"
          :showButtonBar="true"
          placeholder="Select date"
          :invalid="submitted && !localPayment.paidAt"
          class="w-full"
        />
        <small v-if="submitted && !localPayment.paidAt" class="text-red-500"
          >Payment date is required.</small
        >
      </div>

      <!-- Notes -->
      <div>
        <label for="notes" class="mb-2 block font-bold">Notes</label>
        <Textarea id="notes" v-model="localPayment.notes" rows="3" class="w-full" />
      </div>
    </div>

    <template #footer>
      <Button label="Cancel" icon="pi pi-times" text @click="onCancel" />
      <Button label="Save" icon="pi pi-check" :loading="props.isProcessing" @click="onSave" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { ref, watch, computed } from 'vue';
  // import { PAYMENT_METHODS, type Payment } from '@/../../shared/types';

  const props = defineProps<{
    visible: boolean;
    payment: Partial<any>; // Replace 'Payment' with the actual type when available
    isProcessing: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'update:visible', visible: boolean): void;
    (e: 'save', payment: any): void; // Replace 'any' with 'Payment' when the type is available
    (e: 'cancel'): void;
  }>();

  const localPayment = ref<Partial<any>>({ ...props.payment }); // Replace 'any' with 'Payment' when the type is available
  const submitted = ref(false);

  watch(
    () => props.visible,
    (newVal) => {
      if (newVal) {
        submitted.value = false;
        localPayment.value = { ...props.payment };
      }
    },
  );

  // const paymentOptions = PAYMENT_METHODS.map((method) => ({ label: method, value: method }));

  // Validation
  const isAmountValid = computed(
    () =>
      localPayment.value.amount !== null &&
      localPayment.value.amount !== undefined &&
      localPayment.value.amount > 0,
  );
  const isFormValid = computed(
    () => isAmountValid.value && !!localPayment.value.method && !!localPayment.value.paidAt,
  );

  function onSave() {
    submitted.value = true;
    if (!isFormValid.value) return;

    emit('save', { ...localPayment.value } as any); // Replace 'any' with 'Payment' when the type is available
  }

  function onCancel() {
    emit('cancel');
    emit('update:visible', false);
  }
</script>
