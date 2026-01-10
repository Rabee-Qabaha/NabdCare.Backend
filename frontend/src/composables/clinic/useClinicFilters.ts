// src/composables/clinic/useClinicFilters.ts
import type { ClinicResponseDto } from '@/types/backend';
import { computed, reactive, type Ref } from 'vue';

export function useClinicFilters(clinics: Ref<ClinicResponseDto[]>) {
  const activeFilters = reactive({
    global: '',
    status: null as string | null,
  });

  const filteredClinics = computed(() => {
    let data = clinics.value || [];

    if (activeFilters.status) {
      data = data.filter((c) => c.status === activeFilters.status);
    }

    if (activeFilters.global) {
      const q = activeFilters.global.toLowerCase();
      data = data.filter(
        (c) =>
          c.name.toLowerCase().includes(q) ||
          c.email.toLowerCase().includes(q) ||
          c.slug.toLowerCase().includes(q),
      );
    }

    return data;
  });

  return { activeFilters, filteredClinics };
}
