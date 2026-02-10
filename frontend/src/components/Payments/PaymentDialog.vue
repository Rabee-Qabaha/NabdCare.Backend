<template>
  <BaseDrawer
    :visible="visible"
    title="Add Payment"
    icon="pi pi-wallet"
    width="md:!w-[1000px]"
    :dismissable="false"
    @update:visible="emit('update:visible', $event)"
    @close="onCancel"
  >
    <div class="flex flex-col gap-8 pb-20">
      <div
        v-if="props.invoiceId"
        class="bg-primary-50 dark:bg-primary-900/20 p-4 rounded-xl flex items-center gap-4 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800"
      >
        <div class="bg-primary-100 p-2 rounded-full">
          <i class="pi pi-file text-xl"></i>
        </div>
        <div>
          <span class="font-bold block text-lg">Paying for Invoice #{{ invoiceId }}</span>
          <span class="text-sm opacity-80">
            Payments will be allocated to this invoice automatically.
          </span>
        </div>
      </div>

      <div
        v-for="(group, groupIndex) in paymentGroups"
        :key="group.tempId"
        class="relative p-6 border rounded-2xl bg-white dark:bg-surface-900 border-surface-200 dark:border-surface-700 shadow-sm"
      >
        <div class="flex justify-between items-start mb-8">
          <div class="flex items-center gap-3">
            <div
              class="bg-primary-50 text-primary-600 p-2.5 rounded-xl flex items-center justify-center"
            >
              <i
                :class="group.method === PaymentMethod.Cheque ? 'pi pi-book' : 'pi pi-money-bill'"
                class="text-xl"
              ></i>
            </div>
            <div>
              <div class="flex items-center gap-3">
                <h2 class="text-xl font-bold text-surface-900 dark:text-surface-0">
                  Payment #{{ groupIndex + 1 }}
                </h2>
                <span
                  class="bg-amber-100 text-amber-700 text-[10px] font-bold px-2 py-0.5 rounded border border-amber-200 tracking-wider"
                >
                  DRAFT
                </span>
              </div>
            </div>
          </div>
          <Button
            v-if="paymentGroups.length > 1"
            icon="pi pi-trash"
            text
            rounded
            severity="secondary"
            class="text-surface-400 hover:text-red-500"
            @click="removePaymentGroup(groupIndex)"
          />
        </div>

        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <div class="w-full">
            <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
              Total Amount
            </label>
            <div class="relative w-full">
              <span
                class="absolute left-3 top-1/2 -translate-y-1/2 text-surface-500 font-medium text-sm z-10"
              >
                ₪
              </span>
              <InputNumber
                v-model="group.totalAmount"
                mode="decimal"
                :minFractionDigits="2"
                :maxFractionDigits="2"
                locale="en-US"
                placeholder="0.00"
                class="w-full"
                :class="{
                  '!border-red-300 focus:!ring-red-200':
                    !isGroupAmountValid(group) && group.method === PaymentMethod.Cheque,
                }"
                inputClass="w-full pl-8 font-bold text-lg"
              />
            </div>
          </div>

          <div class="w-full">
            <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
              Payment Method
            </label>
            <Select
              v-model="group.method"
              :options="paymentMethodOptions"
              option-label="label"
              option-value="value"
              class="w-full"
              @change="onMethodChange(group)"
            />
          </div>

          <div class="w-full">
            <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
              Payment Date
            </label>
            <DatePicker
              v-model="group.paymentDate"
              show-icon
              fluid
              icon-display="input"
              date-format="dd/mm/yy"
              class="w-full"
              inputClass="w-full"
            />
          </div>
        </div>

        <div
          v-if="group.method === PaymentMethod.Cheque"
          class="bg-primary-50 dark:bg-primary-900/10 border border-surface-100 dark:border-primary-800 rounded-xl p-6 mb-6"
        >
          <div class="flex items-center gap-2 mb-6 text-primary-600 dark:text-primary-400">
            <i class="pi pi-wallet text-sm"></i>
            <h3 class="font-bold text-xs tracking-wider uppercase">Cheque Information</h3>
          </div>

          <div class="flex flex-col gap-6">
            <div
              v-for="(cheque, cIndex) in group.chequeItems"
              :key="cIndex"
              class="bg-white dark:bg-surface-800 border border-surface-200 dark:border-surface-700 rounded-xl p-6 relative group hover:border-primary-300 transition-all shadow-sm"
            >
              <div
                class="absolute -top-3 left-6 bg-surface-100 dark:bg-surface-700 text-surface-600 dark:text-surface-300 text-[10px] font-bold px-2.5 py-1 rounded-full border border-surface-200 dark:border-surface-600"
              >
                #{{ cIndex + 1 }}
              </div>

              <button
                v-if="group.chequeItems.length > 1"
                class="absolute top-4 right-4 text-surface-300 hover:text-red-500 transition-colors"
                @click="removeCheque(group, cIndex)"
              >
                <i class="pi pi-times text-sm"></i>
              </button>

              <div class="grid grid-cols-1 md:grid-cols-4 gap-5 mt-2">
                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Cheque #
                  </label>
                  <InputText
                    v-model="cheque.chequeNumber"
                    class="w-full"
                    placeholder="e.g. 882310"
                  />
                </div>
                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Amount
                  </label>
                  <div class="relative w-full">
                    <span
                      class="absolute left-3 top-1/2 -translate-y-1/2 text-surface-500 text-sm font-medium z-10"
                    >
                      ₪
                    </span>
                    <InputNumber
                      v-model="cheque.amount"
                      mode="decimal"
                      :minFractionDigits="2"
                      class="w-full"
                      inputClass="w-full pl-8"
                      placeholder="0.00"
                    />
                  </div>
                </div>
                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Bank
                  </label>
                  <InputText v-model="cheque.bankName" class="w-full" placeholder="Bank Name" />
                </div>
                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Branch
                  </label>
                  <InputText v-model="cheque.branch" class="w-full" placeholder="Branch" />
                </div>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-4 gap-5 mt-5">
                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Due Date
                  </label>
                  <DatePicker
                    v-model="cheque.dueDate"
                    show-icon
                    fluid
                    icon-display="input"
                    date-format="dd/mm/yy"
                    class="w-full"
                    inputClass="w-full"
                    placeholder="dd/mm/yyyy"
                  />
                </div>

                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Issue Date
                  </label>
                  <DatePicker
                    v-model="cheque.issueDate"
                    show-icon
                    fluid
                    icon-display="input"
                    date-format="dd/mm/yy"
                    class="w-full"
                    inputClass="w-full"
                    placeholder="dd/mm/yyyy"
                  />
                </div>

                <div class="md:col-span-2 w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Cheque Note
                  </label>
                  <InputText v-model="cheque.note" class="w-full" placeholder="Ref # or note..." />
                </div>
              </div>

              <div
                class="mt-6 flex items-center justify-between p-3 rounded-lg border border-dashed border-surface-300 dark:border-surface-600 bg-white dark:bg-surface-800"
              >
                <div class="flex items-center gap-3">
                  <i class="pi pi-paperclip text-surface-400 text-lg -rotate-45"></i>
                  <span
                    v-if="cheque.imageUrl"
                    class="text-sm text-surface-700 dark:text-surface-300 font-medium"
                  >
                    {{ cheque.imageUrl }}
                  </span>
                  <span v-else class="text-sm text-surface-400 dark:text-surface-500 italic">
                    cheque_scan_001.pdf (Mock)
                  </span>
                </div>
                <Button
                  label="Update Attachment"
                  icon="pi pi-cloud-upload"
                  text
                  size="small"
                  class="!text-primary-600 hover:!text-primary-700 !p-0 font-bold"
                />
              </div>
            </div>
          </div>

          <div class="mt-6 flex flex-col md:flex-row justify-between items-center gap-4">
            <div
              v-if="!isGroupAmountValid(group)"
              class="text-xs text-red-500 flex items-center gap-2"
            >
              <i class="pi pi-exclamation-circle"></i>
              <span class="font-medium">
                Cheques total: {{ formatCurrency(getGroupChequeTotal(group)) }} (Diff:
                {{ formatCurrency((group.totalAmount || 0) - getGroupChequeTotal(group)) }})
              </span>
            </div>
            <div v-else></div>

            <Button
              label="Add Cheque"
              icon="pi pi-plus"
              class="!bg-primary-50 !text-primary-600 !border-primary-600 !border-opacity-20 hover:!bg-primary-100 font-bold px-6"
              @click="addChequeToGroup(group)"
            />
          </div>
        </div>

        <div class="border-t border-surface-100 dark:border-surface-700 pt-6">
          <label class="block text-[10px] font-bold text-surface-500 uppercase mb-3">
            Payment Remarks
          </label>
          <Textarea
            v-model="group.notes"
            rows="2"
            autoResize
            class="w-full !bg-surface-50 dark:!bg-surface-800 text-sm !border-surface-200 dark:!border-surface-700"
            placeholder="Add additional notes about this payment..."
          />
        </div>
      </div>

      <Button
        label="Add Another Payment"
        icon="pi pi-plus"
        severity="secondary"
        outlined
        class="w-full border-dashed py-4 font-bold text-surface-500 hover:text-surface-700 hover:border-surface-400"
        @click="addNewPaymentGroup"
      />
    </div>

    <template #footer>
      <div class="flex justify-between items-center w-full">
        <div class="text-sm font-medium text-surface-500">
          Total to Pay:
          <span class="text-surface-900 dark:text-surface-0 font-bold text-lg ml-2">
            {{ formatCurrency(grandTotal) }}
          </span>
        </div>
        <div class="flex gap-3">
          <Button
            label="Cancel"
            icon="pi pi-times"
            text
            severity="secondary"
            class="font-bold"
            @click="onCancel"
          />
          <Button
            label="Save Payment"
            icon="pi pi-check"
            class="font-bold px-6"
            :loading="props.isProcessing"
            :disabled="!isGlobalFormValid"
            @click="onSave"
          />
        </div>
      </div>
    </template>
  </BaseDrawer>
</template>

<script setup lang="ts">
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import { usePaymentActions } from '@/composables/query/payments/usePaymentActions.ts';
  import { BatchPaymentRequestDto } from '@/types/backend/batch-payment-request-dto';
  import { CreateChequeDetailDto } from '@/types/backend/create-cheque-detail-dto';
  import { CreatePaymentRequestDto } from '@/types/backend/create-payment-request-dto';
  import { PaymentAllocationRequestDto } from '@/types/backend/payment-allocation-request-dto';
  import { PaymentContext } from '@/types/backend/payment-context';
  import { PaymentMethod } from '@/types/backend/payment-method';
  import DatePicker from 'primevue/datepicker';
  import { computed, ref, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    isProcessing: boolean;
    clinicId: string;
    invoiceId?: string;
    maxAmount?: number;
  }>();

  const emit = defineEmits<{
    (e: 'update:visible', visible: boolean): void;
    (e: 'refresh'): void;
    (e: 'cancel'): void;
  }>();

  const { createBatch } = usePaymentActions();

  // --- Local Types for UI ---
  // Using a local state wrapper to handle UI-specific fields like tempId and totalAmount
  // while reusing backend DTO types for the rest.
  interface PaymentGroupState {
    tempId: number;
    totalAmount: number | null;
    method: PaymentMethod;
    paymentDate: Date;
    notes: string;
    chequeItems: CreateChequeDetailDto[];
  }

  // --- State ---
  const paymentGroups = ref<PaymentGroupState[]>([]);
  const submitted = ref(false);

  const paymentMethodOptions = Object.values(PaymentMethod).map((m) => ({
    label: m,
    value: m,
  }));

  // --- Initialization ---
  const createNewGroup = (amount = 0): PaymentGroupState => ({
    tempId: Date.now() + Math.random(),
    totalAmount: amount || null,
    method: PaymentMethod.Cheque,
    paymentDate: new Date(),
    notes: '',
    chequeItems: [createEmptyCheque(amount)],
  });

  const createEmptyCheque = (amount = 0): CreateChequeDetailDto => {
    const dto = new CreateChequeDetailDto();
    dto.chequeNumber = '';
    dto.amount = amount || 0;
    dto.bankName = '';
    dto.branch = '';
    dto.dueDate = new Date();
    dto.issueDate = new Date();
    dto.note = '';
    dto.imageUrl = '';
    return dto;
  };

  watch(
    () => props.visible,
    (val) => {
      if (val) {
        submitted.value = false;
        paymentGroups.value = [createNewGroup(props.maxAmount || 0)];
      }
    },
  );

  // --- Interactions ---
  const onMethodChange = (group: PaymentGroupState) => {
    if (group.method === PaymentMethod.Cheque && group.chequeItems.length === 0) {
      group.chequeItems.push(createEmptyCheque(group.totalAmount || 0));
    }
  };

  const addNewPaymentGroup = () => {
    paymentGroups.value.push(createNewGroup(0));
  };

  const removePaymentGroup = (index: number) => {
    paymentGroups.value.splice(index, 1);
  };

  const addChequeToGroup = (group: PaymentGroupState) => {
    const currentAllocated = getGroupChequeTotal(group);
    const remaining = Math.max(0, (group.totalAmount || 0) - currentAllocated);
    group.chequeItems.push(createEmptyCheque(remaining));
  };

  const removeCheque = (group: PaymentGroupState, index: number) => {
    group.chequeItems.splice(index, 1);
  };

  // --- Computed / Validation ---
  const getGroupChequeTotal = (group: PaymentGroupState) => {
    return group.chequeItems.reduce((sum, c) => sum + (c.amount || 0), 0);
  };

  const isGroupAmountValid = (group: PaymentGroupState) => {
    const total = group.totalAmount || 0;
    if (total <= 0) return false;
    if (group.method !== PaymentMethod.Cheque) return true;
    const diff = Math.abs(total - getGroupChequeTotal(group));
    return diff < 0.01;
  };

  const isGlobalFormValid = computed(() => {
    if (paymentGroups.value.length === 0) return false;
    return paymentGroups.value.every((g) => {
      if (!isGroupAmountValid(g)) return false;
      if (g.method === PaymentMethod.Cheque) {
        return g.chequeItems.every(
          (c) =>
            !!c.chequeNumber && !!c.bankName && (c.amount || 0) > 0 && !!c.dueDate && !!c.issueDate,
        );
      }
      return true;
    });
  });

  const grandTotal = computed(() => {
    return paymentGroups.value.reduce((sum, g) => sum + (g.totalAmount || 0), 0);
  });

  const formatCurrency = (val: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'ILS' }).format(val);

  // --- Save Logic ---
  const onSave = () => {
    submitted.value = true;
    if (!isGlobalFormValid.value) return;

    const batchDto = new BatchPaymentRequestDto();
    batchDto.clinicId = props.clinicId;
    batchDto.patientId = undefined as any; // Required by DTO but optional in backend for Clinic context
    batchDto.payments = [];

    paymentGroups.value.forEach((group) => {
      if (group.method === PaymentMethod.Cheque) {
        group.chequeItems.forEach((cItem) => {
          const dto = createBasePaymentDto(group, cItem.amount || 0);
          // Map properties - mostly direct now as we use the DTO
          dto.chequeDetail = cItem;
          // Ensure amounts are synced if UI allows divergence
          dto.chequeDetail.amount = cItem.amount || 0;

          batchDto.payments.push(dto);
        });
      } else {
        batchDto.payments.push(createBasePaymentDto(group, group.totalAmount || 0));
      }
    });

    if (props.invoiceId) {
      const allocation = new PaymentAllocationRequestDto();
      allocation.invoiceId = props.invoiceId;
      allocation.amount = grandTotal.value;
      batchDto.invoicesToPay = [allocation];
    }

    createBatch(batchDto, { onSuccess: successHandler });
  };

  const createBasePaymentDto = (
    group: PaymentGroupState,
    amount: number,
  ): CreatePaymentRequestDto => {
    return {
      amount: amount,
      method: group.method,
      paymentDate: group.paymentDate,
      context: PaymentContext.Clinic,
      clinicId: props.clinicId,
      patientId: undefined as any,
      notes: group.notes,
      allocations: [],
      transactionId: undefined as any,
      chequeDetail: undefined as any,
    } as CreatePaymentRequestDto;
  };

  const successHandler = () => {
    emit('update:visible', false);
    emit('refresh');
  };

  const onCancel = () => {
    emit('update:visible', false);
    emit('cancel');
  };
</script>
