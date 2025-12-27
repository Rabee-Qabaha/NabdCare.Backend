import { InvoiceStatus } from '@/types/backend';
import { computed, reactive } from 'vue';

export function useInvoiceFilters() {
  const activeFilters = reactive({
    global: '',
    status: null as InvoiceStatus | null,
    dateRange: null as Date[] | null,
  });

  const hasActiveFilters = computed(() => {
    return !!activeFilters.global || activeFilters.status !== null || !!activeFilters.dateRange;
  });

  const resetFilters = () => {
    activeFilters.global = '';
    activeFilters.status = null;
    activeFilters.dateRange = null;
  };

  return { activeFilters, hasActiveFilters, resetFilters };
}
