import { paymentsApi } from '@/api/modules/payments';
import type { PaymentDto, PaymentMethod, PaymentStatus } from '@/types/backend';
import { reactive, ref, watch, type Ref } from 'vue';

export function usePaymentsTable(clinicId: Ref<string | undefined>) {
  // 1. Virtual State
  const virtualPayments = ref<PaymentDto[]>(Array.from({ length: 20 }));
  const totalRecords = ref(0);
  const loading = ref(false);
  const nextCursor = ref<string | null>(null);

  // 2. Filter State
  const getInitialFilters = () => ({
    global: '',
    reference: '',
    method: null as PaymentMethod | null,
    status: null as PaymentStatus | null,
    dateRange: null as Date[] | null, // [Start, End]

    // Legacy mapping (cleanup if unused)
    minAmount: null as number | null,
    maxAmount: null as number | null,
    fromDate: null as Date | null,
    toDate: null as Date | null,
    methods: null as string[] | null,
    statuses: null as string[] | null,
  });

  const filters = ref(getInitialFilters());

  const sortState = reactive({
    field: 'paymentDate',
    order: -1, // desc
  });

  let debounceTimer: ReturnType<typeof setTimeout> | null = null;

  // Internal state to track the latest request
  const fetchId = ref(0);

  // 3. Load Data Logic
  const loadData = async (first: number, limit: number) => {
    if (!clinicId.value) return;
    loading.value = true;
    const currentFetchId = ++fetchId.value;

    try {
      const globalSearch = filters.value.global || '';

      const params: any = {
        clinicId: clinicId.value,
        Limit: limit,
        Cursor: first === 0 ? '' : nextCursor.value,
        SortBy: sortState.field,
        Descending: sortState.order === -1,

        Filter: globalSearch,

        // Mapped DTO Filters
        Reference: filters.value.reference,
        Method: filters.value.method,
        Status: filters.value.status,
        StartDate: filters.value.dateRange?.[0],
        EndDate: filters.value.dateRange?.[1],
      };

      const data = await paymentsApi.getPaged(params);

      // Race Condition Check:
      // If a newer request has started since we began, discard this result.
      if (currentFetchId !== fetchId.value) return;

      totalRecords.value = data.totalCount;

      // Resize virtual array if total changed
      if (virtualPayments.value.length !== data.totalCount) {
        // Create array of undefined but preserve existing loaded items if possible
        const newArr = new Array(data.totalCount).fill(undefined);
        virtualPayments.value.forEach((v, k) => {
          if (v && k < newArr.length) newArr[k] = v;
        });
        virtualPayments.value = newArr;
      }

      // Populate Data
      data.items.forEach((item, index) => {
        if (first + index < virtualPayments.value.length) {
          virtualPayments.value[first + index] = item;
        }
      });

      nextCursor.value = data.nextCursor || null;
    } catch (err) {
      if (currentFetchId === fetchId.value) {
        console.error('Failed to load payments table data:', err);
      }
    } finally {
      // Only turn off loading if this was the latest request
      if (currentFetchId === fetchId.value) {
        loading.value = false;
      }
    }
  };

  // Triggered by PrimeVue DataTable lazy event
  const loadPaymentsLazy = (event: any) => {
    const { first, last } = event;
    const limit = last - first;

    // AVOID: Calling loadData here if filter/sort just changed.
    // The watchers on 'filters' and 'sortState' will already trigger a refresh.
    // If we call loadData here too, we get double/triple requests.
    if (event.filterChanged || event.sortChanged) {
      // Just update internal cursor state if needed, but DO NOT fetch.
      // The refresh() called by watchers will handle the fetch (with debounce/reset).
      return;
    }

    // BLOCK: If we are already loading something, don't cascade another request from the scroller
    if (loading.value) return;

    // BLOCK: If the data at this position is already loaded, don't re-fetch.
    // This prevents the redundant fetch after resize (e.g. Limit=7 request).
    if (virtualPayments.value[first] !== undefined) return;

    loadData(first, limit);
  };

  // Watch Global Search Debounce AND other filters
  watch(
    filters,
    () => {
      if (debounceTimer) clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        refresh();
      }, 400);
    },
    { deep: true },
  );

  const onSort = (event: any) => {
    // PrimeVue triggers onSort -> which triggers loadLazy -> which might trigger this.
    // We update state here. The watcher or loadLazy should handle the fetch.
    if (sortState.field === event.sortField && sortState.order === event.sortOrder) return;

    sortState.field = event.sortField;
    sortState.order = event.sortOrder;
    refresh();
  };

  const refresh = () => {
    nextCursor.value = null;
    totalRecords.value = 0; // Optimistic reset
    virtualPayments.value = Array.from({ length: 20 });
    loading.value = false; // Reset loading state to allow new fetch
    loadData(0, 20); // Initial Load
  };

  const clearFilters = () => {
    // This will trigger the deep watcher on 'filters'
    filters.value = getInitialFilters();
    // No need to call refresh() here explicitly if the watcher covers it.
  };

  const applyFilters = (newFilters: any) => {
    // This will trigger the deep watcher on 'filters'
    Object.assign(filters.value, newFilters);
  };

  return {
    virtualPayments,
    totalRecords,
    loading,
    filters,
    loadPaymentsLazy,
    onSort,
    refresh,
    clearFilters,
    applyFilters,
  };
}
