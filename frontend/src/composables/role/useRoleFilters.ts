// src/composables/role/useRoleFilters.ts
import type { RoleResponseDto } from '@/types/backend';
import { computed, reactive, type Ref } from 'vue';

export function useRoleFilters(roles: Ref<RoleResponseDto[]>) {
  const activeFilters = reactive({
    global: '',
    name: '',
    isSystem: null as boolean | null,
    isTemplate: null as boolean | null,
    status: 'active' as string | null, // 'active' | 'deleted' | 'all'
    dateRange: null as Date[] | null,
  });

  const filteredRoles = computed(() => {
    let data = roles.value || [];

    // 1. Status Filter
    if (!activeFilters.status || activeFilters.status === 'active') {
      data = data.filter((r) => !r.isDeleted);
    } else if (activeFilters.status === 'deleted') {
      data = data.filter((r) => r.isDeleted);
    }
    // If 'all', we show everything

    // 2. Role Type
    if (activeFilters.isSystem !== null) {
      data = data.filter((r) => r.isSystemRole === activeFilters.isSystem);
    }

    // 3. Template Filter
    if (activeFilters.isTemplate !== null) {
      data = data.filter((r) => r.isTemplate === activeFilters.isTemplate);
    }

    // 4. Name Search
    if (activeFilters.name) {
      const n = activeFilters.name.toLowerCase();
      data = data.filter((r) => r.name.toLowerCase().includes(n));
    }

    // 5. Global Search
    if (activeFilters.global) {
      const g = activeFilters.global.toLowerCase();
      data = data.filter(
        (r) => r.name.toLowerCase().includes(g) || (r.description || '').toLowerCase().includes(g),
      );
    }

    // 6. Date Range Filter (Robust Version)
    if (Array.isArray(activeFilters.dateRange) && activeFilters.dateRange.length === 2) {
      const start = activeFilters.dateRange[0];
      const end = activeFilters.dateRange[1];

      // Only filter if BOTH dates are selected (PrimeVue sends [Date, null] during selection)
      if (start && end) {
        // Clone to avoid mutating the reactive state
        const startDate = new Date(start);
        startDate.setHours(0, 0, 0, 0); // Start of day

        const endDate = new Date(end);
        endDate.setHours(23, 59, 59, 999); // End of day

        data = data.filter((r) => {
          if (!r.createdAt) return false;

          const created = new Date(r.createdAt);

          // Check for invalid date string
          if (isNaN(created.getTime())) return false;

          return created >= startDate && created <= endDate;
        });
      }
    }

    return data;
  });

  const resetFilters = () => {
    activeFilters.global = '';
    activeFilters.name = '';
    activeFilters.isSystem = null;
    activeFilters.isTemplate = null;
    activeFilters.status = 'active';
    activeFilters.dateRange = null;
  };

  return {
    activeFilters,
    filteredRoles,
    resetFilters,
  };
}
