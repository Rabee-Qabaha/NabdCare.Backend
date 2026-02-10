```vue
<script setup lang="ts">
  import ConfirmDialog from '@/components/shared/ConfirmDialog.vue';
  import { useInvoiceActions } from '@/composables/query/invoices/useInvoiceActions';
  import { useInfiniteInvoicesPaged } from '@/composables/query/invoices/useInvoices';
  import { usePaymentActions } from '@/composables/query/payments/usePaymentActions';
  import { usePermissions } from '@/composables/query/permissions/usePermissions';
  import { useConfirmDialog } from '@/composables/useConfirmDialog';
  import { InvoiceStatus, InvoiceType, Invoices, type InvoiceDto } from '@/types/backend';
  import { formatCurrency, formatDate } from '@/utils/uiHelpers';
  import { computed, onMounted, onUnmounted, ref, watch } from 'vue';

  // UI Components
  import Button from 'primevue/button';
  import DataTable from 'primevue/datatable';
  import Dialog from 'primevue/dialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';
  // Dynamic import for PaymentDialog to avoid circular deps if any
  import PaymentDialog from '@/components/Payments/PaymentDialog.vue';
  import InvoiceDocument from '@/components/Subscription/InvoiceDocument.vue';

  type UiInvoice = InvoiceDto & { _skeleton?: boolean };

  const props = withDefaults(
    defineProps<{
      clinicId?: string;
      subscriptionId?: string;
      showAllocations?: boolean;
      layout?: 'card' | 'list';
      search?: string;
    }>(),
    {
      layout: 'card',
      search: undefined,
    },
  );

  const emit = defineEmits(['view-all', 'update:total-records']);

  // -- Permissions --
  const { can } = usePermissions();
  const canWriteOff = computed(() => can(Invoices.writeOff));

  // -- State --
  const internalSearchQuery = ref('');
  const showPrintDialog = ref(false);
  const selectedInvoice = ref<InvoiceDto | null>(null);
  const expandedRows = ref({});

  // Use external search if provided
  const activeSearch = computed(() =>
    props.search !== undefined ? props.search : internalSearchQuery.value,
  );

  // 👁️ Observer Refs
  const scrollContainer = ref<HTMLElement | null>(null);
  const loadTrigger = ref<HTMLElement | null>(null);
  const isTriggerVisible = ref(false);
  let observer: IntersectionObserver | null = null;

  const companyInfo = {
    name: 'NabdCare SaaS',
    address: '123 Healthcare Ave, Medical City',
    email: 'billing@nabdcare.com',
  };

  // -- Data Fetching --
  const { data, fetchNextPage, hasNextPage, isFetching, isFetchingNextPage, refetch } =
    useInfiniteInvoicesPaged({
      clinicId: computed(() => props.clinicId || null),
      subscriptionId: computed(() => props.subscriptionId || null),
      search: activeSearch,
      status: null,
      limit: 10,
    });

  // -- Computed Data --
  const displayInvoices = computed<UiInvoice[]>(() => {
    const items = data.value?.pages.flatMap((page) => page.items) || [];

    if (isFetching.value && items.length === 0) {
      return Array.from(
        { length: 5 },
        (_, i) => ({ id: `init-${i}`, _skeleton: true }) as UiInvoice,
      );
    }

    if (isFetchingNextPage.value) {
      return [
        ...items,
        ...Array.from({ length: 3 }, (_, i) => ({ id: `lazy-${i}`, _skeleton: true }) as UiInvoice),
      ];
    }

    return items;
  });

  const totalRecords = computed(() => data.value?.pages[0]?.totalCount || 0);

  watch(totalRecords, (val) => {
    emit('update:total-records', val);
  });

  onMounted(() => {
    setTimeout(() => {
      if (!loadTrigger.value || !scrollContainer.value) return;

      observer = new IntersectionObserver(
        (entries) => {
          const entry = entries[0];
          if (entry) {
            isTriggerVisible.value = entry.isIntersecting;
          }
        },
        {
          root: props.layout === 'card' ? scrollContainer.value : null,
          threshold: 0.1,
          rootMargin: '50px',
        },
      );

      observer.observe(loadTrigger.value);
    }, 500);
  });

  watch(
    [isTriggerVisible, hasNextPage, isFetching, isFetchingNextPage],
    ([visible, hasNext, fetching, fetchingNext]) => {
      if (visible && hasNext && !fetching && !fetchingNext) {
        fetchNextPage();
      }
    },
  );

  onUnmounted(() => {
    if (observer) observer.disconnect();
  });

  // -- Actions --
  const { writeOffInvoice, isWritingOff } = useInvoiceActions();
  const { showConfirm, confirmDialogProps, handleConfirm, handleCancel } = useConfirmDialog();

  const openPrintPreview = (invoice: InvoiceDto) => {
    if ((invoice as UiInvoice)._skeleton) return;
    selectedInvoice.value = invoice;
    showPrintDialog.value = true;
  };

  const handleWriteOff = (invoice: InvoiceDto) => {
    showConfirm({
      title: 'Confirm Write Off',
      message: `Are you sure you want to write off invoice #${invoice.invoiceNumber}? This action cannot be undone.`,
      confirmLabel: 'Write Off',
      severity: 'danger',
      onConfirm: () => {
        writeOffInvoice(
          { id: invoice.id, reason: 'Manual Write Off via Admin Dashboard' },
          {
            onSuccess: () => {
              refetch();
            },
          },
        );
      },
    });
  };

  // -- Payment Dialog Logic --
  const showPaymentDialog = ref(false);
  const invoiceToPay = ref<InvoiceDto | null>(null);
  const { createPayment, isCreating } = usePaymentActions();

  const openPaymentDialog = (invoice: InvoiceDto) => {
    invoiceToPay.value = invoice;
    showPaymentDialog.value = true;
  };

  const handlePaymentSave = (paymentData: any) => {
    createPayment(paymentData, {
      onSuccess: () => {
        showPaymentDialog.value = false;
        refetch();
      },
    });
  };

  // -- Helpers --
  const getStatusSeverity = (status: InvoiceStatus | string) => {
    const s = String(status).toLowerCase();
    if (['paid', '2', 'active'].includes(s)) return 'success';
    if (['issued', '1'].includes(s)) return 'warn';
    if (['overdue', '3', 'void'].includes(s)) return 'danger';
    return 'info';
  };

  const getStatusLabel = (status: InvoiceStatus) => InvoiceStatus[status] || status;

  const getTypeLabel = (type: InvoiceType | string) => {
    return String(type)
      .replace(/([A-Z])/g, ' $1')
      .trim();
  };

  defineExpose({
    refetch,
    clearFilters: () => {
      internalSearchQuery.value = '';
    },
  });
</script>

<template>
  <div class="flex flex-col h-full">
    <div
      v-if="props.layout === 'card'"
      class="px-4 py-3 border-b border-surface-200 dark:border-surface-700 flex justify-between items-center shrink-0 bg-surface-50 dark:bg-surface-900 rounded-t-xl"
    >
      <div class="flex items-center gap-2">
        <i class="pi pi-receipt text-surface-500 text-sm"></i>
        <span class="text-sm font-bold text-surface-900 dark:text-surface-0">Billing History</span>
      </div>
      <div class="flex items-center gap-2">
        <IconField icon-position="left">
          <InputIcon class="pi pi-search text-surface-400 !text-xs" />
          <InputText
            v-model="internalSearchQuery"
            placeholder="Search #..."
            class="!w-36 focus:!w-48 transition-all !py-1.5 !text-xs !h-8"
          />
        </IconField>
        <Button
          v-tooltip.bottom="'View All Invoices'"
          icon="pi pi-external-link"
          text
          rounded
          size="small"
          class="!w-8 !h-8 !text-surface-500"
          @click="emit('view-all')"
        />
      </div>
    </div>

    <div
      ref="scrollContainer"
      class="flex-grow overflow-y-auto relative custom-scrollbar"
      :class="props.layout === 'card' ? 'min-h-[300px]' : ''"
    >
      <DataTable
        v-model:expanded-rows="expandedRows"
        :value="displayInvoices"
        class="text-md w-full"
        striped-rows
        row-hover
        data-key="id"
      >
        <template #empty>
          <div class="p-8 text-center flex flex-col items-center gap-3 text-surface-500">
            <i class="pi pi-file-excel text-4xl opacity-50"></i>
            <span>No invoices found.</span>
          </div>
        </template>

        <Column v-if="showAllocations" expander style="width: 2rem" />

        <Column header="Details" header-class="text-left" body-class="text-left">
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex flex-col gap-1.5">
              <Skeleton width="70%" height="0.8rem" />
              <Skeleton width="40%" height="0.6rem" />
            </div>
            <div v-else class="flex flex-col gap-0.5">
              <span
                class="font-mono font-bold text-primary-600 dark:text-primary-400 cursor-pointer hover:underline truncate text-[11px]"
                @click="openPrintPreview(data)"
              >
                {{ data.invoiceNumber }}
              </span>
              <div class="flex items-center gap-1.5 text-[10px] text-surface-500">
                <span class="font-medium text-surface-700 dark:text-surface-300">
                  {{ getTypeLabel(data.type) }}
                </span>
                <span v-if="data.items?.length" class="text-surface-400">
                  • {{ data.items.length }} Items
                </span>
              </div>
            </div>
          </template>
        </Column>

        <Column header="Timeline" style="width: 160px">
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex flex-col gap-1">
              <Skeleton width="60%" height="0.7rem" />
              <Skeleton width="50%" height="0.7rem" />
            </div>
            <div v-else class="flex flex-col gap-0.5 text-[10px]">
              <div class="flex">
                <span class="text-surface-500 mr-2">Issued:</span>
                <span class="font-medium text-surface-700 dark:text-surface-200">
                  {{ formatDate(data.issueDate) }}
                </span>
              </div>
              <div v-if="data.paidDate" class="flex">
                <span class="text-surface-500 mr-2">Paid:</span>
                <span class="font-medium text-green-600 dark:text-green-400">
                  {{ formatDate(data.paidDate) }}
                </span>
              </div>
              <div v-else class="flex">
                <span class="text-surface-500 mr-2">Due:</span>
                <span
                  class="font-bold"
                  :class="
                    new Date(data.dueDate) < new Date()
                      ? 'text-red-500'
                      : 'text-surface-700 dark:text-surface-200'
                  "
                >
                  {{ formatDate(data.dueDate) }}
                </span>
              </div>
            </div>
          </template>
        </Column>

        <Column header="Amount" style="width: 120px">
          body-class="text-left" >
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex flex-col gap-1 items-end">
              <Skeleton width="50%" height="0.8rem" />
              <Skeleton width="30%" height="0.6rem" />
            </div>
            <div v-else class="flex flex-col text-left">
              <span class="font-bold text-surface-900 dark:text-surface-0">
                {{ formatCurrency(data.totalAmount, data.currency) }}
              </span>
              <span v-if="data.balanceDue > 0">
                <span class="mr-2">Due:</span>
                <span class="text-[10px] text-red-500 font-bold">
                  {{ formatCurrency(data.balanceDue, data.currency) }}
                </span>
              </span>
              <span
                v-else
                class="text-[10px] text-green-600 font-medium flex items-center justify-start gap-1"
              >
                <i class="pi pi-check text-[9px]"></i>
                Paid
              </span>
            </div>
          </template>
        </Column>

        <Column header="Status" style="width: 100px" class="text-center" header-class="text-center">
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex justify-center">
              <Skeleton width="100%" height="1.2rem" border-radius="4px" />
            </div>
            <div v-else class="flex justify-center">
              <Tag
                :value="getStatusLabel(data.status)"
                :severity="getStatusSeverity(data.status)"
                class="!text-[10px] !font-bold !px-2.5 !py-0.5 rounded uppercase tracking-wide"
              />
            </div>
          </template>
        </Column>

        <Column style="width: 80px" body-class="text-end">
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex justify-end">
              <Skeleton shape="circle" size="1.5rem" />
            </div>
            <div v-else class="flex gap-1 justify-end">
              <Button
                v-if="canWriteOff && data.balanceDue > 0 && data.status !== InvoiceStatus.Void"
                v-tooltip.left="'Write Off'"
                icon="pi pi-ban"
                text
                rounded
                size="small"
                severity="danger"
                :loading="isWritingOff"
                @click="handleWriteOff(data)"
              />

              <Button
                v-if="data.status !== InvoiceStatus.Paid && data.balanceDue > 0"
                v-tooltip.left="'Pay Invoice'"
                icon="pi pi-wallet"
                text
                rounded
                size="small"
                class="!w-8 !h-8 text-green-600 hover:text-green-800 dark:text-green-400 dark:hover:text-green-300"
                @click="openPaymentDialog(data)"
              />
              <Button
                v-tooltip.left="'Print Invoice'"
                icon="pi pi-print"
                text
                rounded
                size="small"
                class="!w-8 !h-8 text-surface-500 hover:text-surface-900 dark:hover:text-surface-0"
                @click="openPrintPreview(data)"
              />
            </div>
          </template>
        </Column>

        <template #expansion="{ data }">
          <div class="p-4 bg-surface-50 dark:bg-surface-800 grid grid-cols-1 md:grid-cols-2 gap-6">
            <!-- Invoice Items Section -->
            <div>
              <h4 class="text-xs font-bold mb-2 text-surface-500 uppercase tracking-wide">
                Invoice Items
              </h4>
              <div v-if="data.items?.length" class="flex flex-col gap-2">
                <div
                  v-for="item in data.items"
                  :key="item.id"
                  class="flex justify-between items-center text-xs p-2 bg-white dark:bg-surface-700 rounded border border-surface-200 dark:border-surface-600"
                >
                  <div class="flex flex-col">
                    <span class="font-medium text-surface-900 dark:text-surface-0">
                      {{ item.description }}
                    </span>
                    <span class="text-surface-500" v-if="item.quantity > 1">
                      {{ item.quantity }} x {{ formatCurrency(item.unitPrice, data.currency) }}
                    </span>
                  </div>
                  <div class="font-bold text-surface-900 dark:text-surface-0">
                    {{ formatCurrency(item.total, data.currency) }}
                  </div>
                </div>
              </div>
              <div v-else class="text-xs text-surface-400 italic">No items found.</div>
            </div>

            <!-- Payment Allocations Section -->
            <div>
              <h4 class="text-xs font-bold mb-2 text-surface-500 uppercase tracking-wide">
                Payment Allocations
              </h4>
              <div v-if="data.payments?.length" class="flex flex-col gap-2">
                <div
                  v-for="alloc in data.payments"
                  :key="alloc.id"
                  class="flex justify-between items-center text-xs p-2 bg-white dark:bg-surface-700 rounded border border-surface-200 dark:border-surface-600"
                >
                  <div class="flex gap-4">
                    <div class="flex flex-col">
                      <span class="text-surface-500">Date</span>
                      <span class="font-medium">{{ formatDate(alloc.paymentDate) }}</span>
                    </div>
                    <div class="flex flex-col">
                      <span class="text-surface-500">Method</span>
                      <span class="font-medium">{{ alloc.paymentMethod }}</span>
                    </div>
                    <div class="flex flex-col">
                      <span class="text-surface-500">Ref</span>
                      <span class="font-mono text-surface-600 dark:text-surface-300">
                        {{ alloc.reference || '-' }}
                      </span>
                    </div>
                  </div>
                  <div class="font-bold text-green-600">
                    {{ formatCurrency(alloc.amount, data.currency) }}
                  </div>
                </div>
              </div>
              <div v-else class="text-xs text-surface-400 italic">
                No payments allocated to this invoice.
              </div>
            </div>
          </div>
        </template>
      </DataTable>
    </div>

    <Dialog
      v-model:visible="showPrintDialog"
      maximizable
      modal
      header="Invoice Preview"
      :style="{ width: '100vw', height: '100vh', maxHeight: '100%' }"
      :content-style="{ padding: '0' }"
      append-to="body"
    >
      <InvoiceDocument
        v-if="selectedInvoice"
        :invoice="selectedInvoice"
        :company-info="companyInfo"
        @close="showPrintDialog = false"
      />
    </Dialog>

    <PaymentDialog
      v-if="showPaymentDialog"
      v-model:visible="showPaymentDialog"
      :is-processing="isCreating"
      :clinic-id="props.clinicId!"
      :invoice-id="invoiceToPay?.id"
      :max-amount="invoiceToPay?.balanceDue"
      @save="handlePaymentSave"
    />

    <ConfirmDialog
      :visible="confirmDialogProps.visible"
      :title="confirmDialogProps.title"
      :message="confirmDialogProps.message"
      :severity="confirmDialogProps.severity"
      :confirm-label="confirmDialogProps.confirmLabel"
      @update:visible="handleCancel"
      @confirm="handleConfirm"
      @cancel="handleCancel"
    />
  </div>
</template>
```
