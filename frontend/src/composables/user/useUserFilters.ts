// src/composables/user/useUserFilters.ts
import { ref, computed, type Ref } from 'vue';
import type { UserResponseDto } from '@/types/backend';
import { FilterMatchMode, FilterOperator } from '@primevue/core/api';

export function useUserFilters(users: Ref<UserResponseDto[]>) {
  const filters = ref<Record<string, any>>({
    global: { value: null, matchMode: FilterMatchMode.CONTAINS },
    fullName: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    email: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    roleName: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    clinicName: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
    },
    isActive: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
    },
    createdAt: {
      operator: FilterOperator.AND,
      constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
    },
  });

  const showDeleted = ref(false);

  const filteredUsers = computed(() => {
    const list = users.value || [];
    const f = filters.value;

    const global = f.global.value;
    const fullName = f.fullName.constraints[0].value;
    const email = f.email.constraints[0].value;
    const roleName = f.roleName.constraints[0].value;
    const clinicName = f.clinicName.constraints[0].value;
    const status = f.isActive.constraints[0].value;
    const createdDate = f.createdAt.constraints[0].value;

    return list.filter((u) => {
      const globalMatch = !global
        ? true
        : [u.fullName, u.email, u.roleName, u.clinicName].some(
            (val) => val?.toLowerCase().includes(global.toLowerCase()),
          );

      const fullNameMatch = !fullName
        ? true
        : u.fullName?.toLowerCase().includes(fullName.toLowerCase());

      const emailMatch = !email ? true : u.email?.toLowerCase().includes(email.toLowerCase());

      const roleMatch = !roleName
        ? true
        : u.roleName?.toLowerCase().includes(roleName.toLowerCase());

      const clinicMatch = !clinicName
        ? true
        : (u.clinicName?.toLowerCase() ?? '').includes(clinicName.toLowerCase());

      const statusMatch = status === null || status === undefined ? true : u.isActive === status;

      const dateMatch = !createdDate
        ? true
        : new Date(u.createdAt).toDateString() === new Date(createdDate).toDateString();

      return (
        globalMatch &&
        fullNameMatch &&
        emailMatch &&
        roleMatch &&
        clinicMatch &&
        statusMatch &&
        dateMatch
      );
    });
  });

  const resetFilters = () => {
    filters.value = {
      global: { value: null, matchMode: FilterMatchMode.CONTAINS },
      fullName: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
      },
      email: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
      },
      roleName: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
      },
      clinicName: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.CONTAINS }],
      },
      isActive: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.EQUALS }],
      },
      createdAt: {
        operator: FilterOperator.AND,
        constraints: [{ value: null, matchMode: FilterMatchMode.DATE_IS }],
      },
    };
    showDeleted.value = false;
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
    showDeleted,
    filteredUsers,
    resetFilters,
    clearFilter,
  };
}