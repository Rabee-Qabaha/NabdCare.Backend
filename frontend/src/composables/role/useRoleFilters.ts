import { ref, computed, type Ref } from 'vue';
import type { RoleResponseDto } from '@/types/backend';
import { FilterMatchMode, FilterOperator } from '@primevue/core/api';

export function useRoleFilters(roles: Ref<RoleResponseDto[]>) {
  const filters = ref<Record<string, any>>({
    global: { value: null, matchMode: FilterMatchMode.CONTAINS },
    name: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    description: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    category: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
    },
    isDeleted: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
    },
    createdAt: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
    },
  });

  const filteredRoles = computed(() => {
    const list = roles.value || [];
    const f = filters.value;

    const global = f.global.value;
    const name = f.name.constraints[0].value;
    const description = f.description.constraints[0].value;
    const category = f.category.constraints[0].value;
    const isDeleted = f.isDeleted.constraints[0].value;
    const createdDate = f.createdAt.constraints[0].value;

    return list.filter((r) => {
      // Global search across name and description
      const globalMatch = !global
        ? true
        : [r.name, r.description].some(
            (val) => val?.toLowerCase().includes(global.toLowerCase()),
          );

      // Name filter
      const nameMatch = !name ? true : r.name?.toLowerCase().includes(name.toLowerCase());

      // Description filter
      const descriptionMatch = !description
        ? true
        : (r.description?.toLowerCase() ?? '').includes(description.toLowerCase());

      // Category filter: "system" or "clinic"
      const categoryMatch =
        category === null || category === undefined
          ? true
          : (category === 'system' ? r.isSystemRole : !r.isSystemRole);

      // ✅ Deleted status filter
      const deletedMatch =
        isDeleted === null || isDeleted === undefined
          ? true
          : r.isDeleted === isDeleted;

      // Created date filter
      const dateMatch = !createdDate
        ? true
        : new Date(r.createdAt).toDateString() === new Date(createdDate).toDateString();

      return (
        globalMatch &&
        nameMatch &&
        descriptionMatch &&
        categoryMatch &&
        deletedMatch &&
        dateMatch
      );
    });
  });

  const resetFilters = () => {
    filters.value = {
      global: { value: null, matchMode: FilterMatchMode.CONTAINS },
      name: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
      },
      description: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
      },
      category: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
      },
      isDeleted: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
      },
      createdAt: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
      },
    };
  };

  const clearFilter = (filterName: string) => {
    if (filters.value[filterName]) {
      if (filters.value[filterName].constraints) {
        filters.value[filterName].constraints[0].value = null;
      } else {
        filters.value[filterName].value = null;
      }
    }
  };

  return {
    filters,
    filteredRoles,
    resetFilters,
    clearFilter,
  };
}