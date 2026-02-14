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
    <div class="flex flex-col gap-8">
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
        <div class="flex justify-between items-center mb-6">
          <div class="flex items-center gap-3">
            <div
              class="bg-primary-50 text-primary-600 p-2.5 rounded-xl flex items-center justify-center"
            >
              <i class="pi pi-wallet text-xl"></i>
            </div>
            <div>
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

        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <div class="w-full">
            <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
              Total Amount
            </label>
            <div class="relative w-full">
              <InputNumber
                v-model="group.totalAmount"
                mode="decimal"
                locale="en-US"
                placeholder="0.00"
                class="w-full"
                :class="{
                  '!border-red-300 focus:!ring-red-200':
                    !isGroupAmountValid(group) && group.method === PaymentMethod.Cheque,
                }"
                inputClass="w-full font-bold text-lg"
              />
            </div>
          </div>
          <div class="w-full">
            <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
              Currency
            </label>
            <Select v-model="group.currency" :options="supportedCurrencies" class="w-full" />
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

        <!-- Exchange Rate Info -->
        <div
          v-if="group.currency !== functionalCurrency && group.exchangeRate"
          class="flex items-center gap-2 mb-6 text-xs text-surface-500 font-medium"
        >
          <i class="pi pi-sync"></i>
          <span>Exchange Rate @ {{ formatExchangeRate(group.exchangeRate.finalRate) }}</span>
          <span class="mx-1">|</span>
          <span>
            Total in {{ functionalCurrency }} =
            {{ formatFunctionalCurrency(getConvertedAmount(group) || 0) }}
          </span>
          <span v-if="group.exchangeRate.markupValue > 0" class="ml-2 text-[10px] text-surface-400">
            (Includes {{ group.exchangeRate.markupValue }}% fee)
          </span>
        </div>
        <div
          v-else-if="group.currency !== functionalCurrency && !group.exchangeRate"
          class="mb-6 text-xs text-red-500 animate-pulse flex items-center gap-2"
        >
          <i class="pi pi-spin pi-spinner"></i>
          Fetching exchange rate...
        </div>

        <div
          v-if="group.method === PaymentMethod.Cheque"
          class="border border-surface-200 dark:border-surface-700 rounded-xl p-0 mb-6 overflow-hidden"
        >
          <div
            class="bg-primary-50 dark:bg-surface-800/50 px-6 py-3 border-b border-surface-100 dark:border-surface-700 flex items-center gap-2"
          >
            <div
              class="bg-primary-600 text-white w-6 h-6 rounded flex items-center justify-center shadow-sm"
            >
              <i class="pi pi-wallet text-xs"></i>
            </div>
            <h3
              class="font-bold text-xs text-primary-700 dark:text-primary-400 tracking-wider uppercase"
            >
              Cheque Information
            </h3>
          </div>

          <div class="flex flex-col gap-0">
            <div
              v-for="(cheque, cIndex) in group.chequeItems"
              :key="cIndex"
              class="p-6 relative group border-b border-surface-100 dark:border-surface-700 last:border-0"
            >
              <div class="absolute top-6 left-2 -translate-x-full pr-2">
                <span
                  class="bg-surface-100 dark:bg-surface-700 text-surface-500 text-[10px] font-bold px-2 py-0.5 rounded-full"
                >
                  #{{ cIndex + 1 }}
                </span>
              </div>

              <button
                v-if="group.chequeItems.length > 1"
                class="absolute top-4 right-4 text-surface-300 hover:text-red-500 transition-colors"
                @click="removeCheque(group, cIndex)"
              >
                <i class="pi pi-times text-sm"></i>
              </button>

              <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
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
                    Amount & Currency
                  </label>
                  <div class="flex gap-2">
                    <InputNumber
                      v-model="cheque.amount"
                      mode="decimal"
                      locale="en-US"
                      placeholder="0.00"
                      class="w-full"
                    />
                    <Select
                      v-model="cheque.currency"
                      :options="supportedCurrencies"
                      class="w-24 shrink-0"
                      @change="onChequeCurrencyChange(cheque)"
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

              <div
                v-if="cheque.currency !== functionalCurrency && cheque.exchangeRate"
                class="flex items-center gap-2 my-4 text-[10px] text-surface-400 font-medium"
              >
                <i class="pi pi-sync"></i>
                <span>Exchange Rate @ {{ formatExchangeRate(cheque.exchangeRate.finalRate) }}</span>
                <span class="mx-1">|</span>
                <span>
                  Converted =
                  {{
                    formatFunctionalCurrency((cheque.amount || 0) * cheque.exchangeRate.finalRate)
                  }}
                </span>
              </div>
              <div
                v-else-if="cheque.currency !== functionalCurrency && !cheque.exchangeRate"
                class="flex items-center gap-2 my-4 text-[10px] text-red-500 animate-pulse font-medium"
              >
                <i class="pi pi-spin pi-spinner text-xs"></i>
                <span>Fetching exchange rate...</span>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
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

                <div class="w-full">
                  <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
                    Cheque Note
                  </label>
                  <InputText v-model="cheque.note" class="w-full" placeholder="Ref # or note..." />
                </div>
              </div>

              <div class="mt-4 flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <i class="pi pi-paperclip text-surface-400 text-lg -rotate-45"></i>
                  <span
                    v-if="cheque.imageUrl"
                    class="text-sm text-surface-700 dark:text-surface-300 font-medium"
                  >
                    {{ cheque.imageUrl }}
                  </span>
                  <span v-else class="text-sm text-surface-500 dark:text-surface-400">
                    cheque_scan_001.pdf
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

          <div
            class="px-6 py-4 bg-surface-50 dark:bg-surface-800 border-t border-surface-200 dark:border-surface-700 flex flex-col md:flex-row justify-end items-center gap-4"
          >
            <div
              v-if="!isGroupAmountValid(group)"
              class="text-xs text-red-500 flex items-center gap-2 mr-auto"
            >
              <i class="pi pi-exclamation-circle"></i>
              <span class="font-medium">
                Total Cheques Value:
                {{ formatFunctionalCurrency(getGroupChequeTotalInFunctional(group)) }}
              </span>
            </div>
            <div v-else class="text-xs text-surface-500 flex items-center gap-2 mr-auto">
              <i class="pi pi-check-circle text-green-500"></i>
              <span class="font-medium">
                Total Cheques Value:
                {{ formatFunctionalCurrency(getGroupChequeTotalInFunctional(group)) }}
              </span>
            </div>

            <Button
              label="ADD CHEQUE"
              icon="pi pi-plus"
              text
              class="!bg-primary-50 hover:!bg-primary-100 !text-primary-600 !border !border-primary-200 font-bold px-4 py-2"
              @click="addChequeToGroup(group)"
            />
          </div>
        </div>

        <div class="mt-6">
          <label class="block text-[10px] font-bold text-surface-500 uppercase mb-2">
            Payment Remarks
          </label>
          <Textarea
            v-model="group.notes"
            rows="3"
            autoResize
            class="w-full !bg-white dark:!bg-surface-900 border !border-surface-300 dark:!border-surface-600 focus:!border-primary-500 p-3 rounded-xl text-sm"
            placeholder="Enter overall payment remarks or instructions here..."
          />
        </div>
      </div>

      <div class="mt-4 pt-4 border-t border-dashed border-surface-200 dark:border-surface-700">
        <Button
          label="Add Another Payment"
          icon="pi pi-plus"
          severity="secondary"
          outlined
          class="w-full py-3 font-bold !bg-primary-50 hover:!bg-primary-100 !text-primary-600 !border-primary-200"
          @click="addNewPaymentGroup"
        />
      </div>
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
            :loading="isInternalProcessing"
            :disabled="!isGlobalFormValid"
            @click="onSave"
          />
        </div>
      </div>
    </template>
  </BaseDrawer>
</template>

<script setup lang="ts">
  import { clinicsApi } from '@/api/modules/clinics';
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import { useClinicQueries } from '@/composables/query/clinics/useClinicQueries';
  import { usePaymentActions } from '@/composables/query/payments/usePaymentActions.ts';
  import { useToastService } from '@/composables/useToastService.ts';
  import { BatchPaymentRequestDto } from '@/types/backend/batch-payment-request-dto';
  import { CreateChequeDetailDto } from '@/types/backend/create-cheque-detail-dto';
  import { CreatePaymentRequestDto } from '@/types/backend/create-payment-request-dto';
  import { Currency } from '@/types/backend/currency';
  import { ExchangeRateResponseDto } from '@/types/backend/exchange-rate-response-dto';
  import { PaymentAllocationRequestDto } from '@/types/backend/payment-allocation-request-dto';
  import { PaymentContext } from '@/types/backend/payment-context';
  import { PaymentMethod } from '@/types/backend/payment-method';
  import DatePicker from 'primevue/datepicker';
  import Select from 'primevue/select';
  import { computed, ref, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    isProcessing: boolean;
    clinicId: string;
    invoiceId?: string;
    maxAmount?: number;
    defaultCurrency?: string;
  }>();

  const emit = defineEmits<{
    (e: 'update:visible', visible: boolean): void;
    (e: 'refresh'): void;
    (e: 'cancel'): void;
  }>();

  const { createBatch, isCreatingBatch } = usePaymentActions();
  const toast = useToastService();

  // Use internal loading state since we handle the mutation here
  const isInternalProcessing = computed(() => isCreatingBatch.value || props.isProcessing);

  // --- Local Types for UI ---
  // Extended type for Cheque to include its own rate and nullable amount for UI
  interface ChequeUIState extends Omit<CreateChequeDetailDto, 'amount'> {
    amount: number | null;
    exchangeRate?: ExchangeRateResponseDto | null;
  }

  interface PaymentGroupState {
    tempId: number;
    totalAmount: number | null;
    method: PaymentMethod;
    currency: Currency;
    exchangeRate?: ExchangeRateResponseDto | null;
    paymentDate: Date;
    notes: string;
    chequeItems: ChequeUIState[];
  }

  // --- State ---
  const paymentGroups = ref<PaymentGroupState[]>([]);
  const submitted = ref(false);

  const paymentMethodOptions = Object.values(PaymentMethod).map((m) => ({
    label: m,
    value: m,
  }));

  // --- Initialization ---
  const createNewGroup = (amount = 0): PaymentGroupState => {
    const group: PaymentGroupState = {
      tempId: Date.now() + Math.random(),
      totalAmount: amount || null,
      method: PaymentMethod.Cheque,
      currency: (props.defaultCurrency as Currency) || functionalCurrency.value,
      paymentDate: new Date(),
      notes: '',
      chequeItems: [createEmptyCheque(amount)],
    };

    // Initial fetch for the cheque if needed
    const firstCheque = group.chequeItems[0];
    if (firstCheque && firstCheque.currency !== functionalCurrency.value) {
      fetchExchangeRateForCheque(firstCheque);
    }

    return group;
  };

  const createEmptyCheque = (amount: number | null = null): ChequeUIState => {
    const dto = new CreateChequeDetailDto();
    // Default properties
    dto.chequeNumber = '';
    dto.currency = (props.defaultCurrency as Currency) || functionalCurrency.value;
    dto.bankName = '';
    dto.branch = '';
    dto.dueDate = new Date();
    dto.issueDate = new Date();
    dto.note = '';
    dto.imageUrl = '';

    // Cast to UI state
    const uiState = dto as unknown as ChequeUIState;
    uiState.amount = amount || null; // If 0, becomes null -> empty input
    uiState.exchangeRate = null;

    return uiState;
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

  // --- Exchange Rate Logic ---
  import { useConfiguration } from '@/composables/useConfiguration';

  const { useClinicDetails } = useClinicQueries();
  const { functionalCurrency: systemCurrency } = useConfiguration();

  const clinicIdRef = computed(() => props.clinicId);
  const { data: clinic } = useClinicDetails(clinicIdRef);

  const functionalCurrency = computed(
    () => clinic.value?.settings?.currency || systemCurrency.value || Currency.USD,
  );
  const supportedCurrencies = Object.values(Currency);

  // Helper to fetch rate for a specific group
  const fetchExchangeRateForGroup = async (group: PaymentGroupState) => {
    if (!clinic.value?.settings?.currency) return;

    // Reset if same as functional
    if (group.currency === functionalCurrency.value) {
      group.exchangeRate = null;
      return;
    }

    try {
      const rate = await clinicsApi.getExchangeRate(group.currency);
      group.exchangeRate = rate;
    } catch (err) {
      console.error('Failed to fetch exchange rate', err);
      toast.error('Could not fetch exchange rate for ' + group.currency);
      group.exchangeRate = null;
    }
  };

  const fetchExchangeRateForCheque = async (cheque: ChequeUIState) => {
    if (!clinic.value?.settings?.currency) return;

    // Reset if same as functional
    if (cheque.currency === functionalCurrency.value) {
      cheque.exchangeRate = null;
      return;
    }

    try {
      const rate = await clinicsApi.getExchangeRate(cheque.currency);
      cheque.exchangeRate = rate;
    } catch (err) {
      console.error('Failed to fetch exchange rate for cheque', err);
      cheque.exchangeRate = null;
    }
  };

  const getConvertedAmount = (group: PaymentGroupState) => {
    if (!group.exchangeRate || !group.totalAmount) return null;
    return group.totalAmount * group.exchangeRate.finalRate;
  };

  const onChequeCurrencyChange = (cheque: ChequeUIState) => {
    fetchExchangeRateForCheque(cheque);
  };

  // Watch for currency changes in groups
  watch(
    () => paymentGroups.value,
    (groups) => {
      groups.forEach((group) => {
        // Group Rate Logic
        if (
          group.currency !== functionalCurrency.value &&
          (!group.exchangeRate || group.exchangeRate.targetCurrency !== group.currency)
        ) {
          fetchExchangeRateForGroup(group);
        }
        if (group.currency === functionalCurrency.value && group.exchangeRate) {
          group.exchangeRate = null;
        }

        // Ensure Cheques maintain correct rates if they are newly added or implicitly changed?
        // Actually, explicit change is handled by onChequeCurrencyChange.
        // We might want to auto-sync cheque currency if it was never touched?
        // For now let's keep them independent as per UI controls.
      });
    },
    { deep: true },
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

    const newCheque = createEmptyCheque(remaining);
    group.chequeItems.push(newCheque);

    if (newCheque.currency !== functionalCurrency.value) {
      fetchExchangeRateForCheque(newCheque);
    }
  };

  const removeCheque = (group: PaymentGroupState, index: number) => {
    group.chequeItems.splice(index, 1);
  };

  // --- Computed / Validation ---
  const getGroupChequeTotal = (group: PaymentGroupState) => {
    return group.chequeItems.reduce((sum, c) => sum + (c.amount || 0), 0);
  };

  const getGroupChequeTotalInFunctional = (group: PaymentGroupState) => {
    return group.chequeItems.reduce((sum, c) => {
      const amount = c.amount || 0;
      if (c.currency === functionalCurrency.value) return sum + amount;
      if (c.exchangeRate) return sum + amount * c.exchangeRate.finalRate;
      return sum; // Or handle missing rate error?
    }, 0);
  };

  const isGroupAmountValid = (group: PaymentGroupState) => {
    const total = group.totalAmount || 0;
    if (total <= 0) return false;
    if (group.method !== PaymentMethod.Cheque) return true;

    // Convert group total to functional currency for comparison
    let groupTotalInFunctional = total;
    if (group.currency !== functionalCurrency.value && group.exchangeRate) {
      groupTotalInFunctional = total * group.exchangeRate.finalRate;
    }

    const chequeTotalInFunctional = getGroupChequeTotalInFunctional(group);

    // Allow slightly larger epsilon for currency conversion rounding
    const diff = Math.abs(groupTotalInFunctional - chequeTotalInFunctional);
    return diff < 0.05;
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
    return paymentGroups.value.reduce((sum, g) => {
      const amount =
        g.currency === functionalCurrency.value ? g.totalAmount || 0 : getConvertedAmount(g) || 0;
      return sum + amount;
    }, 0);
  });

  const formatCurrency = (val: number, currency = functionalCurrency.value || 'USD') =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(val);

  const formatFunctionalCurrency = (val: number) => formatCurrency(val, functionalCurrency.value);

  const formatExchangeRate = (val: number) => {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 4,
      maximumFractionDigits: 4,
    }).format(val);
  };

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
          // We need to cast or reconstruct because ChequeUIState has amount as number|null
          dto.chequeDetail = {
            ...cItem,
            amount: cItem.amount || 0,
          } as CreateChequeDetailDto;

          // Ensure amounts are synced if UI allows divergence
          // (redundant given above, but safe)
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
    const dto = {
      amount: amount,
      method: group.method,
      currency: group.currency,
      paymentDate: group.paymentDate,
      context: PaymentContext.Clinic,
      clinicId: props.clinicId,
      patientId: undefined as any,
      notes: group.notes,
      allocations: [],
      transactionId: undefined as any,
      chequeDetail: undefined as any,
      baseExchangeRate: group.exchangeRate?.baseRate,
      finalExchangeRate: group.exchangeRate?.finalRate,
    } as unknown as CreatePaymentRequestDto;

    return dto;
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
