<script setup lang="ts">
  import { useInfiniteInvoicesPaged } from '@/composables/query/invoices/useInvoices';
  import { InvoiceStatus, type InvoiceDto } from '@/types/backend';
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
      limit: 5,
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
            console.log(`👁️ Visibility changed: ${entry.isIntersecting}`);
          }
        },
        {
          root: scrollContainer.value,
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
    if (['overdue', '3'].includes(s)) return 'danger';
    return 'info';
  };
  const getStatusLabel = (status: InvoiceStatus) => InvoiceStatus[status] || status;
</script>

<template>
  <div
    class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 rounded-lg flex flex-col h-[350px]"
  >
    <div
      class="px-3 py-2 border-b border-surface-200 dark:border-surface-700 flex justify-between items-center shrink-0 bg-surface-50 dark:bg-surface-900 rounded-t-lg"
    >
      <div class="flex items-center gap-2">
        <i class="pi pi-receipt text-surface-500 text-xs"></i>
        <span class="text-xs font-bold text-surface-900 dark:text-surface-0">Billing History</span>
      </div>
      <div class="flex items-center gap-2">
        <IconField icon-position="left">
          <InputIcon class="pi pi-search text-surface-400 !text-[10px]" />
          <InputText
            v-model="searchQuery"
            placeholder="Search #..."
            class="!w-24 focus:!w-32 transition-all !py-1 !text-[11px] !h-7"
          />
        </IconField>
        <Button
          v-tooltip.left="'View All Invoices'"
          icon="pi pi-external-link"
          text
          rounded
          size="small"
          class="!w-7 !h-7 !text-surface-500"
          @click="emit('view-all')"
        />
      </div>
    </div>

    <div ref="scrollContainer" class="flex-grow overflow-y-auto relative custom-scrollbar">
      <DataTable :value="displayInvoices" class="text-[11px] w-full" striped-rows row-hover>
        <template #empty>
          <div class="p-6 text-center text-surface-500 text-xs">No invoices found.</div>
        </template>

        <Column header="Invoice #" style="min-width: 110px">
          <template #body="{ data }">
            <div v-if="data._skeleton" class="flex flex-col gap-1">
              <Skeleton width="80%" height="0.8rem" />
              <Skeleton width="40%" height="0.6rem" />
            </div>
            <div v-else class="flex flex-col">
              <span
                class="font-mono font-medium text-primary-600 dark:text-primary-400 cursor-pointer hover:underline truncate"
                @click="openPrintPreview(data)"
              >
                {{ data.invoiceNumber }}
              </span>
              <span class="text-[9px] text-surface-400">
                Issued: {{ formatDate(data.issueDate) }}
              </span>
            </div>
          </template>
        </Column>

        <Column header="Amount" style="min-width: 90px">
          <template #body="{ data }">
            <div v-if="data._skeleton"><Skeleton width="60%" height="0.8rem" /></div>
            <div v-else class="flex flex-col">
              <span class="font-bold">{{ formatCurrency(data.totalAmount) }}</span>
              <span v-if="data.balanceDue > 0" class="text-[9px] text-red-500 font-bold">
                Due: {{ formatCurrency(data.balanceDue) }}
              </span>
              <span v-else class="text-[9px] text-green-600 font-medium">Paid Full</span>
            </div>
          </template>
        </Column>

        <Column header="Status" style="width: 80px">
          <template #body="{ data }">
            <div v-if="data._skeleton">
              <Skeleton width="3rem" height="1rem" border-radius="4px" />
            </div>
            <Tag
              v-else
              :value="getStatusLabel(data.status)"
              :severity="getStatusSeverity(data.status)"
              class="text-[9px] uppercase font-bold px-1.5 py-0 h-4 leading-none"
              rounded
            />
          </template>
        </Column>

        <Column style="width: 40px" align-frozen="right" frozen>
          <template #body="{ data }">
            <div v-if="data._skeleton"><Skeleton shape="circle" size="1.2rem" /></div>
            <Button
              v-else
              icon="pi pi-print"
              text
              rounded
              size="small"
              class="!w-6 !h-6"
              severity="secondary"
              @click="openPrintPreview(data)"
            />
          </template>
        </Column>
      </DataTable>

      <div ref="loadTrigger" class="h-8 w-full flex items-center justify-center p-2 mt-1">
        <i v-if="isFetchingNextPage" class="pi pi-spin pi-spinner text-primary-500 text-sm"></i>
        <span
          v-else-if="!hasNextPage && displayInvoices.length > 0"
          class="text-[10px] text-surface-400 italic"
        >
          No more records
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
  </div>
</template>
