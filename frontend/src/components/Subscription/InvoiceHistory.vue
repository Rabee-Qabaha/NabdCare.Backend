<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import { useInfiniteInvoicesPaged } from '@/composables/query/invoices/useInvoices';
  import { InvoiceStatus, InvoiceType, type InvoiceDto } from '@/types/backend';
  import { formatCurrency, formatDate } from '@/utils/uiHelpers';
  import { computed, onMounted, onUnmounted, ref, watch } from 'vue';

  // UI Components
  import InvoiceDocument from '@/components/Subscription/InvoiceDocument.vue';
  import Button from 'primevue/button';
  import Column from 'primevue/column';
  import DataTable from 'primevue/datatable';
  import Dialog from 'primevue/dialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  type UiInvoice = InvoiceDto & { _skeleton?: boolean };

  const props = defineProps<{ clinicId?: string }>();
  const emit = defineEmits(['view-all']);

  // -- State --
  const searchQuery = ref('');
  const showPrintDialog = ref(false);
  const selectedInvoice = ref<InvoiceDto | null>(null);

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
  const { data, fetchNextPage, hasNextPage, isFetching, isFetchingNextPage } =
    useInfiniteInvoicesPaged({
      clinicId: computed(() => props.clinicId || null),
      search: searchQuery,
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
        { root: scrollContainer.value, threshold: 0.1, rootMargin: '50px' },
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
  const openPrintPreview = (invoice: InvoiceDto) => {
    if ((invoice as UiInvoice)._skeleton) return;
    selectedInvoice.value = invoice;
    showPrintDialog.value = true;
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
</script>

<template>
  <BaseCard no-padding class="flex flex-col h-full">
    <div
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
            v-model="searchQuery"
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
      class="flex-grow overflow-y-auto relative custom-scrollbar min-h-[300px]"
    >
      <DataTable :value="displayInvoices" class="text-xs w-full" striped-rows row-hover>
        <template #empty>
          <div class="p-8 text-center flex flex-col items-center gap-3 text-surface-500">
            <i class="pi pi-file-excel text-4xl opacity-50"></i>
            <span>No invoices found.</span>
          </div>
        </template>

        <Column header="Details" headerClass="text-left" bodyClass="text-left">
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

        <Column
          header="Timeline"
          style="width: 160px"
          headerClass="text-left"
          bodyClass="text-left"
        >
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

        <Column header="Amount" style="width: 120px" headerClass="text-left" bodyClass="text-left">
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

        <Column header="Status" style="width: 100px" class="text-center" headerClass="text-center">
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

        <Column style="width: 60px" bodyClass="text-center">
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex justify-center">
              <Skeleton shape="circle" size="1.5rem" />
            </div>
            <div v-else>
              <Button
                icon="pi pi-print"
                text
                rounded
                size="small"
                class="!w-8 !h-8 text-surface-500 hover:text-surface-900 dark:hover:text-surface-0"
                @click="openPrintPreview(data)"
                v-tooltip.left="'Print Invoice'"
              />
            </div>
          </template>
        </Column>
      </DataTable>

      <div ref="loadTrigger" class="h-10 w-full flex items-center justify-center p-2 mt-1">
        <div v-if="isFetchingNextPage" class="flex items-center gap-2 text-surface-500 text-xs">
          <i class="pi pi-spin pi-spinner"></i>
          Loading more...
        </div>
        <span
          v-else-if="!hasNextPage && displayInvoices.length > 0"
          class="text-[10px] text-surface-400 italic"
        >
          End of history
        </span>
      </div>
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
  </BaseCard>
</template>
