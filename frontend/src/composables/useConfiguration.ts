import { configurationApi } from '@/api/modules/configuration';
import { useQuery } from '@tanstack/vue-query';
import { computed } from 'vue';

export const CONFIG_QUERY_KEY = ['systemConfiguration'];

export function useConfiguration() {
  const { data, isLoading, error } = useQuery({
    queryKey: CONFIG_QUERY_KEY,
    queryFn: () => configurationApi.getConfiguration(),
    staleTime: Infinity, // Configuration rarely changes, cache indefinitely
    retry: 2,
  });

  const functionalCurrency = computed(() => data.value?.functionalCurrency || 'USD'); // Fallback to USD

  return {
    configuration: data,
    isLoading,
    error,
    functionalCurrency,
  };
}
