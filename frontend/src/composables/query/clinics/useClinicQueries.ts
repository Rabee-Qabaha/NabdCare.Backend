// src/composables/query/clinics/useClinicQueries.ts
import { clinicsApi } from '@/api/modules/clinics';
import type { ClinicFilterRequestDto } from '@/types/backend';
import { useQuery, keepPreviousData } from '@tanstack/vue-query';
import { computed, type Ref } from 'vue';

export function useClinicQueries() {
  
  // 📋 LIST (Paged)
  const useClinicsList = (params: Ref<ClinicFilterRequestDto>) => {
    return useQuery({
      queryKey: ['clinics', params],
      queryFn: () => clinicsApi.getAllPaged(params.value),
      placeholderData: keepPreviousData, // Keeps list visible while reloading
    });
  };

  // ℹ️ SINGLE DETAILS (For Edit)
  const useClinicDetails = (id: Ref<string>) => {
    return useQuery({
      queryKey: ['clinic', id],
      queryFn: () => clinicsApi.getById(id.value),
      enabled: computed(() => !!id.value),
      staleTime: 1000 * 60 * 5, // 5 minutes
    });
  };

  // 📊 DASHBOARD STATS (New!)
  const useClinicStats = (id: Ref<string>) => {
    return useQuery({
      queryKey: ['clinic-stats', id],
      queryFn: () => clinicsApi.getStats(id.value),
      enabled: computed(() => !!id.value),
      staleTime: 1000 * 60, // 1 minute
    });
  };

  return {
    useClinicsList,
    useClinicDetails,
    useClinicStats
  };
}