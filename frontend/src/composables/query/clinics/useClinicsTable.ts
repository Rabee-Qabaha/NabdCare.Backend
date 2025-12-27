// src/composables/query/clinics/useClinicsTable.ts
import { clinicsApi } from '@/api/modules/clinics';
import {
  SubscriptionStatus,
  type ClinicFilterRequestDto,
  type ClinicResponseDto,
} from '@/types/backend';
import { reactive, ref, watch, type Ref } from 'vue';

export function useClinicsTable(includeDeleted: Ref<boolean>) {
  // 1. Virtual State
  const virtualClinics = ref<ClinicResponseDto[]>(Array.from({ length: 50 }));
  const totalRecords = ref(0);
  const loading = ref(false);
  const nextCursor = ref<string | null>(null);

  // 2. Filter State
  // Matches the shape expected by ClinicFilters.vue
  const getInitialFilters = () => ({
    global: '',
    name: '',
    email: '',
    phone: '',
    status: null as SubscriptionStatus | null,
    subscriptionType: null as number | null,
    subscriptionFee: null as number | null,
    createdAt: null as Date | null,

    // ✅ Added missing fields to fix TS error in ClinicFilters.vue
    minBranches: null as number | null,
    expirationDateRange: null as Date[] | null, // [Start, End]
    createdDateRange: null as Date[] | null, // [Start, End]
  });

  const filters = ref(getInitialFilters());

  const sortState = reactive({
    field: 'createdAt',
    order: -1,
  });

  let debounceTimer: ReturnType<typeof setTimeout> | null = null;

  // 3. Load Data Logic
  const loadData = async (first: number, limit: number) => {
    if (loading.value) return;
    loading.value = true;

    try {
      const globalSearch = filters.value.global || '';

      // Prepare API params
      // Note: Cast as any allows passing extra filters if the backend supports them later
      const params: ClinicFilterRequestDto & Record<string, any> = {
        limit: limit,
        cursor: nextCursor.value ?? '',
        sortBy: sortState.field,
        descending: sortState.order === -1,
        includeDeleted: includeDeleted.value,

        filter: globalSearch,
        search: globalSearch,

        name: filters.value.name || '',
        email: filters.value.email || '',
        phone: filters.value.phone || '',

        // Enum/Number Filters
        status: (filters.value.status ?? null) as any,
        subscriptionType: (filters.value.subscriptionType ?? null) as any,
        subscriptionFee: (filters.value.subscriptionFee ?? null) as any,
        createdAt: (filters.value.createdAt ?? null) as any,

        // Range Filters (Mapped for Backend)
        minBranches: filters.value.minBranches,
        expirationFrom: filters.value.expirationDateRange?.[0],
        expirationTo: filters.value.expirationDateRange?.[1],
        createdFrom: filters.value.createdDateRange?.[0],
        createdTo: filters.value.createdDateRange?.[1],
      };

      const data = await clinicsApi.getAllPaged(params);

      totalRecords.value = data.totalCount;

      // Handle Virtual Scroll Array Resizing
      if (virtualClinics.value.length !== data.totalCount) {
        const newArr = new Array(data.totalCount).fill(undefined);
        virtualClinics.value.forEach((v, k) => {
          if (v && k < newArr.length) newArr[k] = v;
        });
        virtualClinics.value = newArr;
      }

      // Populate Data into Virtual Array
      data.items.forEach((item, index) => {
        if (first + index < virtualClinics.value.length) {
          virtualClinics.value[first + index] = item;
        }
      });

      nextCursor.value = data.nextCursor || null;
    } catch (err) {
      console.error('Failed to load clinics table data:', err);
    } finally {
      loading.value = false;
    }
  };

  // Triggered by PrimeVue DataTable lazy event
  const loadCarsLazy = (event: any) => {
    const { first, last } = event;
    const limit = last - first;

    // Reset data if filters or sort changed (avoids stale data gaps)
    if (event.filterChanged || event.sortChanged) {
      nextCursor.value = null;
      virtualClinics.value = Array.from({ length: 50 });
    }

    loadData(first, limit);
  };

  // Watch Global Search Debounce
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
    sortState.field = event.sortField;
    sortState.order = event.sortOrder;
    refresh();
  };

  const refresh = () => {
    nextCursor.value = null;
    // Reset virtual array to trigger re-render and Skeleton state
    virtualClinics.value = Array.from({ length: 50 });
    loadData(0, 20); // Initial Load
  };

  const clearFilters = () => {
    filters.value = getInitialFilters();
    refresh();
  };

  const applyFilters = (newFilters: any) => {
    Object.assign(filters.value, newFilters);
    // Watcher will trigger refresh automatically
  };

  return {
    virtualClinics,
    totalRecords,
    loading,
    filters,
    loadCarsLazy,
    onSort,
    refresh,
    clearFilters,
    applyFilters,
  };
}
