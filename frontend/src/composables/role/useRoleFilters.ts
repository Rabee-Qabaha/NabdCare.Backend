import { reactive, toRefs } from 'vue';

// Define the shape of our filter state
export interface RoleFiltersState {
  global: string; // Search text
  roleOrigin: string | null; // 'system' | 'clinic' | null
  isTemplate: boolean | null;
  status: string | null; // 'active' | 'deleted' | 'all'
  dateRange: Date[] | null;
}

export function useRoleFilters() {
  // Reactive state to hold filter values
  const state = reactive<RoleFiltersState>({
    global: '',
    roleOrigin: null,
    isTemplate: null,
    status: 'active',
    dateRange: null,
  });

  // Reset function to clear all filters
  const resetFilters = () => {
    state.global = '';
    state.roleOrigin = null;
    state.isTemplate = null;
    state.status = 'active';
    state.dateRange = null;
  };

  return {
    ...toRefs(state),
    filtersState: state,
    resetFilters,
  };
}
