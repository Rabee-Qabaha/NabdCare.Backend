// Path: src/composables/query/invoices/useInvoices.ts
import { invoicesApi } from '@/api/modules/invoices';
import type {
  InvoiceDto,
  InvoiceListRequestDto,
  InvoiceStatus,
  PaginatedResult,
} from '@/types/backend';
import { keepPreviousData, useInfiniteQuery, useQuery } from '@tanstack/vue-query';
import { computed, unref, type Ref } from 'vue';

function buildParams(params: {
  clinicId?: string | null;
  status?: InvoiceStatus | null;
  search?: string;
  fromDate?: Date | null;
  toDate?: Date | null;
  cursor?: string | null;
  limit?: number;
}): InvoiceListRequestDto {
  const queryParams = {
    limit: params.limit ?? 20,
    cursor: params.cursor || undefined,
    sortBy: 'issueDate',
    descending: true,

    clinicId: params.clinicId || undefined,
    invoiceNumber: params.search || undefined,
    status: params.status || undefined,
    fromDate: params.fromDate || undefined,
    toDate: params.toDate || undefined,

    subscriptionId: undefined,
    filter: undefined,
    type: undefined,
  };

  return queryParams as unknown as InvoiceListRequestDto;
}

export function useInfiniteInvoicesPaged(options: {
  clinicId?: Ref<string | null> | string | null;
  status?: Ref<InvoiceStatus | null> | InvoiceStatus | null;
  search?: Ref<string> | string;
  dateRange?: Ref<Date[] | null> | Date[] | null;
  limit?: number;
}) {
  const clinicId = computed(() => unref(options.clinicId));
  const status = computed(() => unref(options.status));
  const search = computed(() => unref(options.search));
  const dateRange = computed(() => unref(options.dateRange));

  const limit = options.limit ?? 20;

  const queryKey = computed(() => [
    'invoices',
    'infinite',
    {
      clinicId: clinicId.value,
      status: status.value,
      search: search.value,
      dateRange: dateRange.value,
      limit: limit,
    },
  ]);

  return useInfiniteQuery<PaginatedResult<InvoiceDto>>({
    queryKey,
    queryFn: async ({ pageParam }) => {
      const fromDate = dateRange.value?.[0] || null;
      const toDate = dateRange.value?.[1] || null;

      return await invoicesApi.getPaged(
        buildParams({
          clinicId: clinicId.value,
          status: status.value,
          search: search.value,
          fromDate,
          toDate,
          cursor: pageParam as string | null,
          limit: limit,
        }),
      );
    },
    initialPageParam: null,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    staleTime: 0,
    gcTime: 1000 * 60 * 5,
    refetchOnWindowFocus: true,
    placeholderData: keepPreviousData,
  });
}

export function useInvoice(id: Ref<string | null>) {
  return useQuery({
    queryKey: computed(() => ['invoice', id.value]),
    queryFn: async () => {
      if (!id.value) return null;
      return await invoicesApi.getById(id.value);
    },
    // Only fetch if we have an ID
    enabled: computed(() => !!id.value),
    staleTime: 1000 * 60 * 30, // Cache detail for 30 mins
  });
}
