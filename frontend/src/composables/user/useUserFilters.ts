// src/composables/user/useUserFilters.ts
import type { UserResponseDto } from '@/types/backend';
import { computed, reactive, type Ref } from 'vue';

export function useUserFilters(users: Ref<UserResponseDto[]>) {
  const activeFilters = reactive({
    global: '',
    roleId: null as string | null,
    clinicId: null as string | null,
    isActive: null as boolean | null,
    status: 'active' as string | null,
    dateRange: null as Date[] | null,
  });

  const filteredUsers = computed(() => {
    let data = users.value || [];

    // 1. Deleted Status
    if (activeFilters.status === 'deleted') {
      data = data.filter((u) => u.isDeleted);
    } else if (activeFilters.status === 'all') {
      // No filter
    } else {
      data = data.filter((u) => !u.isDeleted);
    }

    // 2. Account Status
    if (activeFilters.isActive !== null) {
      data = data.filter((u) => u.isActive === activeFilters.isActive);
    }

    // 3. Role
    if (activeFilters.roleId) {
      data = data.filter((u) => u.roleId === activeFilters.roleId);
    }

    // 4. Clinic
    if (activeFilters.clinicId) {
      data = data.filter((u) => u.clinicId === activeFilters.clinicId);
    }

    // 5. Global Search
    if (activeFilters.global) {
      const q = activeFilters.global.toLowerCase();
      data = data.filter(
        (u) =>
          u.fullName.toLowerCase().includes(q) ||
          u.email.toLowerCase().includes(q) ||
          u.roleName?.toLowerCase().includes(q),
      );
    }

    // 6. Date Range
    if (activeFilters.dateRange && activeFilters.dateRange.length === 2) {
      const [start, end] = activeFilters.dateRange;
      if (start && end) {
        const startDate = new Date(start);
        startDate.setHours(0, 0, 0, 0);
        const endDate = new Date(end);
        endDate.setHours(23, 59, 59, 999);
        data = data.filter((u) => {
          if (!u.createdAt) return false;
          const d = new Date(u.createdAt);
          return d >= startDate && d <= endDate;
        });
      }
    }

    return data;
  });

  const resetFilters = () => {
    activeFilters.global = '';
    activeFilters.roleId = null;
    activeFilters.clinicId = null;
    activeFilters.isActive = null;
    activeFilters.status = 'active';
    activeFilters.dateRange = null;
  };

  return { activeFilters, filteredUsers, resetFilters };
}
