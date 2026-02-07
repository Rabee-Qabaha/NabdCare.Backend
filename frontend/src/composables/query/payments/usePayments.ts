import { paymentsApi } from '@/api/modules/payments';
import { keepPreviousData, useQuery } from '@tanstack/vue-query';
import { computed, type Ref } from 'vue';

export function usePayments(clinicId: Ref<string | undefined>, queryParams: Ref<any>) {
  const isEnabled = computed(() => !!clinicId.value);

  const queryKey = computed(() => ['payments', clinicId.value, { ...queryParams.value }]);

  const query = useQuery({
    queryKey,
    queryFn: () => {
      const params = queryParams.value || {};
      return paymentsApi.getPaged({
        clinicId: clinicId.value,
        Limit: params.pageSize || 10,
        Filter: params.search,
        SortBy: params.orderBy,
        Descending: params.descending,
        Cursor: params.cursor,
      });
    },
    enabled: isEnabled,
    placeholderData: keepPreviousData,
  });

  const payments = computed(() => query.data.value?.items ?? []);
  const totalCount = computed(() => query.data.value?.totalCount ?? 0);
  const isLoading = computed(() => query.isLoading.value);

  return {
    payments,
    totalCount,
    isLoading,
    query,
  };
}
