<script setup lang="ts">
  import { ref, computed, watch, onBeforeUnmount, onMounted } from 'vue';
  import { useToast } from 'primevue/usetoast';
  import { useConfirm } from 'primevue/useconfirm';
  import {
    useInfiniteUsersPaged,
    useCreateUser,
    useUpdateUser,
  } from '@/composables/query/users/useUsers';
  import {
    useResetPassword,
    useActivateUser,
    useDeactivateUser,
    useSoftDeleteUser,
    useHardDeleteUser,
    useRestoreUser,
  } from '@/composables/query/users/useUserActions';
  import { useRoles, useClinics } from '@/composables/query/useDropdownData';
  import UserDialog from '@/components/User/UserDialog.vue';
  import ChangePasswordDialog from '@/components/User/ChangePasswordDialog.vue';
  import EmptyState from '@/components/EmptyState.vue';
  import { formatDate } from '@/utils/uiHelpers';
  import { usePermission } from '@/composables/usePermission';
  import { FilterMatchMode, FilterOperator } from '@primevue/core/api';
  import type { UserResponseDto } from '@/types/backend';
  import RoleSelect from '@/components/Dropdowns/RoleSelect.vue';
  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';

  // PrimeVue Components
  import InputText from 'primevue/inputtext';
  import Select from 'primevue/select';
  import DatePicker from 'primevue/datepicker';
  import Checkbox from 'primevue/checkbox';
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import Tag from 'primevue/tag';
  import Avatar from 'primevue/avatar';
  import Dialog from 'primevue/dialog';
  import ConfirmDialog from 'primevue/confirmdialog';
  import ProgressSpinner from 'primevue/progressspinner';
  import Skeleton from 'primevue/skeleton';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';

  /**
   * Users Management Page
   * Location: src/views/pages/admin/Users.vue
   *
   * Features:
   * ✅ User CRUD operations (create, read, update, delete)
   * ✅ Advanced filtering and global search
   * ✅ Infinite scroll pagination
   * ✅ Bulk actions (activate, deactivate, delete)
   * ✅ Password reset functionality
   * ✅ Checkbox selection
   * ✅ Vue Query + SWR caching (automatic background refresh)
   * ✅ Role-based permission control
   *
   * Architecture:
   * - Vue Query composables for data fetching & mutations
   * - Service layer for API abstraction
   * - Composables for permissions and dropdowns
   * - Client-side filtering and search
   * - Infinite scroll with intersection observer
   *
   * Author: Rabee Qabaha
   * Updated: 2025-11-02
   */

  // ========================================
  // COMPOSABLES & SERVICES
  // ========================================

  const toast = useToast();
  const confirm = useConfirm();
  const { can: canPermission } = usePermission();

  // ========================================
  // FILTER CONFIGURATION
  // ========================================

  const statusOptions = ref([
    { label: 'Active', value: true, severity: 'success' },
    { label: 'Inactive', value: false, severity: 'danger' },
  ]);

  const initialFilters = {
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

  // ========================================
  // DROPDOWN DATA (Cached)
  // ========================================

  useRoles();
  useClinics();

  // ========================================
  // STATE MANAGEMENT
  // ========================================

  const filters = ref(structuredClone(initialFilters));
  const filtersDialogVisible = ref(false);
  const userDialog = ref(false);
  const changePasswordDialog = ref(false);
  const deleteUserDialog = ref(false);
  const bulkActionDialog = ref(false);
  const softDeleteLoading = ref(false);
  const showDeleted = ref(false);

  const currentBulkAction = ref<'activate' | 'deactivate' | 'delete' | null>(null);
  const user = ref<Partial<UserResponseDto>>({});
  const selectedUserIds = ref<number[]>([]);
  const loadingUserIds = ref<Set<string>>(new Set());
  const bulkActionLoading = ref(false);

  // ========================================
  // INFINITE SCROLL
  // ========================================

  const BATCH_SIZE = 20;
  const visibleCount = ref(BATCH_SIZE);
  const loadingMore = ref(false);
  const sentinel = ref<HTMLElement | null>(null);
  let observer: IntersectionObserver | null = null;

  // ========================================
  // PERMISSIONS
  // ========================================

  const canCreate = computed(() => canPermission('Users.Create'));
  const canEdit = computed(() => canPermission('Users.Edit'));
  const canDelete = computed(() => canPermission('Users.Delete'));
  const canResetPassword = computed(() => canPermission('Users.ResetPassword'));
  const canActivate = computed(() => canPermission('Users.Activate'));

  // ========================================
  // QUERIES & MUTATIONS
  // ========================================

  const {
    data: infiniteData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isFetching,
    refetch,
    error,
    isError,
  } = useInfiniteUsersPaged({
    search: '',
    includeDeleted: showDeleted,
  });

  // CRUD Mutations
  const createUserMutation = useCreateUser();
  const updateUserMutation = useUpdateUser();

  // Action Mutations (from useUserActions)
  const softDeleteUserMutation = useSoftDeleteUser();
  const hardDeleteUserMutation = useHardDeleteUser();
  const resetPasswordMutation = useResetPassword();
  const activateUserMutation = useActivateUser();
  const deactivateUserMutation = useDeactivateUser();
  const restoreUserMutation = useRestoreUser();

  // ========================================
  // COMPUTED PROPERTIES
  // ========================================

  const allUsers = computed(() => {
    if (!infiniteData.value?.pages) return [];
    return infiniteData.value.pages.flatMap((page) => page.items || []);
  });

  const filteredUsers = computed(() => {
    const list = allUsers.value || [];
    const f = filters.value;

    const global = f.global.value;
    const name = f.fullName.constraints[0].value;
    const email = f.email.constraints[0].value;
    const role = f.roleName.constraints[0].value;
    const clinic = f.clinicName.constraints[0].value;
    const status = f.isActive.constraints[0].value;
    const created = f.createdAt.constraints[0].value;

    return list.filter((u) => {
      const globalPass = global
        ? [u.fullName, u.email, u.roleName, u.clinicName].some((val) => contains(val, global))
        : true;

      return (
        globalPass &&
        contains(u.fullName, name) &&
        contains(u.email, email) &&
        contains(u.roleName, role) &&
        contains(u.clinicName ?? '', clinic) &&
        equals(u.isActive, status) &&
        sameDate(u.createdAt, created)
      );
    });
  });

  const visibleUsers = computed(() => filteredUsers.value.slice(0, visibleCount.value));

  const bulkVisible = computed(() => selectedUserIds.value.length > 0);

  const bulkDialogData = computed(() => {
    const count = selectedUserIds.value.length;
    switch (currentBulkAction.value) {
      case 'activate':
        return {
          header: 'Confirm Bulk Activation',
          actionLabel: 'Activate',
          actionIcon: 'pi pi-check',
          severity: 'success',
          message: `Activate ${count} selected user${count !== 1 ? 's' : ''}?`,
        };
      case 'deactivate':
        return {
          header: 'Confirm Bulk Deactivation',
          actionLabel: 'Deactivate',
          actionIcon: 'pi pi-ban',
          severity: 'warning',
          message: `Deactivate ${count} selected user${count !== 1 ? 's' : ''}?`,
        };
      case 'delete':
        return {
          header: 'Confirm Bulk Deletion',
          actionLabel: 'Delete',
          actionIcon: 'pi pi-trash',
          severity: 'danger',
          message: `Delete ${count} selected user${count !== 1 ? 's' : ''}?`,
        };
      default:
        return {
          header: '',
          actionLabel: '',
          actionIcon: '',
          severity: 'secondary',
          message: '',
        };
    }
  });

  // ========================================
  // HELPER FUNCTIONS
  // ========================================

  function contains(h: any, n: any): boolean {
    if (!n) return true;
    return String(h ?? '')
      .toLowerCase()
      .includes(String(n).toLowerCase());
  }

  function equals(a: any, b: any): boolean {
    if (b === null || b === undefined) return true;
    return a === b;
  }

  function sameDate(a: any, b: any): boolean {
    if (!b) return true;
    if (!a) return false;
    const da = new Date(a);
    const db = new Date(b);
    return da.toDateString() === db.toDateString();
  }

  // ========================================
  // FILTER OPERATIONS
  // ========================================

  function resetFilters(): void {
    filters.value = structuredClone(initialFilters);
    visibleCount.value = BATCH_SIZE;
  }

  function openFilters(): void {
    filtersDialogVisible.value = true;
  }

  function applyFilters(): void {
    filtersDialogVisible.value = false;
    visibleCount.value = BATCH_SIZE;
    refetch();
  }

  function clearFilters(): void {
    resetFilters();
    filtersDialogVisible.value = false;
  }

  // ========================================
  // DIALOG OPERATIONS
  // ========================================

  function openNew(): void {
    user.value = {};
    userDialog.value = true;
  }

  function hideDialog(): void {
    userDialog.value = false;
  }

  function editUser(u: UserResponseDto): void {
    user.value = { ...u };
    userDialog.value = true;
  }

  function openChangePasswordDialog(u: UserResponseDto): void {
    user.value = { ...u };
    changePasswordDialog.value = true;
  }

  function confirmDeleteUser(u: UserResponseDto): void {
    user.value = { ...u };
    deleteUserDialog.value = true;
  }

  // ========================================
  // CRUD OPERATIONS
  // ========================================

  async function saveUser(newUser: any): Promise<void> {
    try {
      if (!newUser.id) {
        await createUserMutation.mutateAsync(newUser);
        toast.add({
          severity: 'success',
          summary: 'Created',
          detail: `User ${newUser.email} created`,
          life: 3000,
        });
      } else {
        await updateUserMutation.mutateAsync({
          id: newUser.id,
          data: newUser,
        });
        toast.add({
          severity: 'success',
          summary: 'Updated',
          detail: 'User updated',
          life: 3000,
        });
      }
      userDialog.value = false;
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Save failed',
        life: 3000,
      });
    }
  }

  async function deleteUser(): Promise<void> {
    if (!user.value.id) return;

    softDeleteLoading.value = true;

    try {
      await softDeleteUserMutation.mutateAsync(user.value.id);
      toast.add({
        severity: 'success',
        summary: 'Deleted',
        detail: 'User deleted successfully',
        life: 3000,
      });
      deleteUserDialog.value = false;
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Failed to delete user',
        life: 3000,
      });
    } finally {
      softDeleteLoading.value = false;
    }
  }

  async function hardDeleteUser(u: UserResponseDto): Promise<void> {
    confirm.require({
      message: `Permanently delete ${u.fullName}? This action cannot be undone.`,
      header: '⚠️ Permanent Deletion',
      icon: 'pi pi-exclamation-triangle',
      rejectLabel: 'Cancel',
      acceptLabel: 'Delete Permanently',
      rejectProps: { outlined: true },
      acceptProps: { severity: 'danger' },
      accept: async () => {
        try {
          await hardDeleteUserMutation.mutateAsync(u.id);
          toast.add({
            severity: 'success',
            summary: 'Deleted',
            detail: 'User permanently deleted',
            life: 3000,
          });
        } catch (err: any) {
          toast.add({
            severity: 'error',
            summary: 'Error',
            detail: err.message || 'Failed to permanently delete user',
            life: 3000,
          });
        }
      },
    });
  }

  async function toggleUserStatus(u: UserResponseDto): Promise<void> {
    try {
      loadingUserIds.value.add(u.id);
      if (u.isActive) {
        await deactivateUserMutation.mutateAsync(u.id);
        toast.add({
          severity: 'success',
          summary: 'Deactivated',
          detail: `${u.fullName} has been deactivated`,
          life: 3000,
        });
      } else {
        await activateUserMutation.mutateAsync(u.id);
        toast.add({
          severity: 'success',
          summary: 'Activated',
          detail: `${u.fullName} has been activated`,
          life: 3000,
        });
      }
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Failed to update user status',
        life: 3000,
      });
    } finally {
      loadingUserIds.value.delete(u.id);
    }
  }

  async function handleChangePassword(data: { newPassword: string }): Promise<void> {
    if (!user.value.id) return;
    try {
      await resetPasswordMutation.mutateAsync({
        id: user.value.id,
        data: { newPassword: data.newPassword } as any,
      });
      toast.add({
        severity: 'success',
        summary: 'Success',
        detail: 'Password reset successfully',
        life: 3000,
      });
      changePasswordDialog.value = false;
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Failed to reset password',
        life: 3000,
      });
    }
  }

  async function confirmRestore(u: UserResponseDto): Promise<void> {
    confirm.require({
      message: `Restore user ${u.fullName} (${u.email})?`,
      header: 'Restore User',
      icon: 'pi pi-refresh',
      rejectLabel: 'Cancel',
      acceptLabel: 'Restore',
      acceptIcon: 'pi pi-undo',
      acceptProps: { severity: 'success' },
      rejectProps: { outlined: true },
      accept: async () => {
        try {
          await restoreUserMutation.mutateAsync(u.id);
          toast.add({
            severity: 'success',
            summary: 'Restored',
            detail: `${u.fullName} has been restored`,
            life: 3000,
          });

          // ✅ Immediately refresh the list
          await refetch();

          // Optional: if you were in "Show Deleted" mode, flip back to active list:
          // showDeleted.value = false;
          // await refetch();
        } catch (err: any) {
          toast.add({
            severity: 'error',
            summary: 'Error',
            detail: err?.message || 'Failed to restore user',
            life: 4000,
          });
        }
      },
    });
  }

  // ========================================
  // BULK ACTIONS
  // ========================================

  function bulkActivate(): void {
    currentBulkAction.value = 'activate';
    bulkActionDialog.value = true;
  }

  function bulkDeactivate(): void {
    currentBulkAction.value = 'deactivate';
    bulkActionDialog.value = true;
  }

  function bulkDelete(): void {
    currentBulkAction.value = 'delete';
    bulkActionDialog.value = true;
  }

  async function handleBulkActionConfirm(): Promise<void> {
    const ids = [...selectedUserIds.value];
    const action = currentBulkAction.value;

    if (!action || !ids.length) return;

    bulkActionDialog.value = false;
    bulkActionLoading.value = true;
    let successCount = 0;
    let errorCount = 0;

    try {
      const getMutation = () => {
        switch (action) {
          case 'activate':
            return activateUserMutation.mutateAsync;
          case 'deactivate':
            return deactivateUserMutation.mutateAsync;
          case 'delete':
            return softDeleteUserMutation.mutateAsync;
          default:
            return null;
        }
      };

      const mutationFn = getMutation();
      if (!mutationFn) return;

      const results = await Promise.allSettled(ids.map((id) => mutationFn(id)));

      results.forEach((result) => {
        if (result.status === 'fulfilled') {
          successCount++;
        } else {
          errorCount++;
        }
      });

      if (successCount > 0) {
        const actionText = {
          activate: 'activated',
          deactivate: 'deactivated',
          delete: 'deleted',
        }[action];

        toast.add({
          severity: 'success',
          summary: 'Success',
          detail:
            errorCount === 0
              ? `${successCount} user${successCount !== 1 ? 's' : ''} ${actionText}`
              : `${successCount} user${successCount !== 1 ? 's' : ''} ${actionText}, ${errorCount} failed`,
          life: 3000,
        });
      }

      if (errorCount > 0) {
        toast.add({
          severity: 'warn',
          summary: 'Partial Failure',
          detail: `${errorCount} user${errorCount !== 1 ? 's' : ''} could not be ${action}d`,
          life: 3000,
        });
      }

      selectedUserIds.value = [];
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Bulk action failed',
        life: 3000,
      });
    } finally {
      bulkActionLoading.value = false; // ✅ NEW: Clear loading state
      currentBulkAction.value = null;
    }
  }

  // ========================================
  // INFINITE SCROLL
  // ========================================

  function setupObserver(): void {
    if (observer) observer.disconnect();
    if (!sentinel.value) return;

    observer = new IntersectionObserver(async (entries) => {
      const [entry] = entries;
      if (!entry.isIntersecting) return;

      if (hasNextPage.value && !isFetchingNextPage.value) {
        loadingMore.value = true;
        await fetchNextPage();
        await new Promise((r) => setTimeout(r, 400));
        visibleCount.value = Math.min(visibleCount.value + BATCH_SIZE, filteredUsers.value.length);
        loadingMore.value = false;
      }
    });

    observer.observe(sentinel.value);
  }

  // ========================================
  // LIFECYCLE HOOKS
  // ========================================

  onMounted(setupObserver);
  onBeforeUnmount(() => observer?.disconnect());
  watch(sentinel, setupObserver);
  watch(
    () => filters.value.global.value,
    () => {
      visibleCount.value = BATCH_SIZE;
    },
  );
</script>

<template>
  <div class="space-y-4 p-4 md:p-6">
    <!-- HEADER -->
    <div>
      <h2 class="text-2xl font-bold md:text-3xl">Users Management</h2>
      <p class="text-600">Manage all users across all clinics</p>
    </div>

    <!-- TOOLBAR -->
    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div class="flex flex-wrap gap-2">
        <Button v-if="canCreate" label="Create User" icon="pi pi-plus" @click="openNew" />
        <template v-if="bulkVisible">
          <Button
            v-if="canActivate"
            label="Activate Selected"
            icon="pi pi-check"
            outlined
            severity="success"
            @click="bulkActivate"
          />
          <Button
            v-if="canActivate"
            label="Deactivate Selected"
            icon="pi pi-ban"
            outlined
            severity="warning"
            @click="bulkDeactivate"
          />
          <Button
            v-if="canDelete"
            label="Delete Selected"
            icon="pi pi-trash"
            outlined
            severity="danger"
            @click="bulkDelete"
          />
        </template>
      </div>

      <div class="flex flex-wrap gap-2">
        <Button icon="pi pi-sliders-h" label="Filters" outlined @click="openFilters" />
        <!-- Toggle Active/Deleted view -->
        <Button
          :label="showDeleted ? 'Show Active' : 'Show Deleted'"
          :icon="showDeleted ? 'pi pi-users' : 'pi pi-trash'"
          outlined
          @click="
            () => {
              showDeleted = !showDeleted;
              visibleCount = BATCH_SIZE;
              refetch();
            }
          "
        />
        <Button icon="pi pi-filter-slash" label="Clear Filters" outlined @click="resetFilters" />
        <Button
          icon="pi pi-refresh"
          label="Refresh"
          outlined
          @click="refetch"
          :loading="isFetching"
        />
      </div>
    </div>

    <!-- SEARCH -->
    <div>
      <IconField class="w-full">
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="filters.global.value"
          placeholder="Search users by name, email, role, or clinic..."
          class="w-full"
        />
      </IconField>
      <small v-if="filters.global.value" class="mt-1 block text-surface-500">
        {{ filteredUsers.length }} result{{ filteredUsers.length !== 1 ? 's' : '' }}
        found
      </small>
    </div>

    <!-- ERROR STATE -->
    <div v-if="isError" class="rounded-lg bg-red-50 p-4 dark:bg-red-900">
      <p class="text-red-800 dark:text-red-200">
        ❌ {{ error?.message || 'Failed to load users' }}
      </p>
      <Button label="Retry" icon="pi pi-refresh" text @click="refetch" class="mt-2" />
    </div>

    <!-- LOADING -->
    <div
      v-if="isFetching && allUsers.length === 0"
      class="grid gap-4 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
    >
      <Card v-for="n in 8" :key="n">
        <template #title>
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-2">
              <Skeleton shape="circle" size="2.5rem" animation="wave" />
              <div class="flex flex-col gap-2">
                <Skeleton width="8rem" height="1rem" animation="wave" />
                <Skeleton width="6rem" height="0.8rem" animation="wave" />
              </div>
            </div>
            <Skeleton width="1.2rem" height="1.2rem" borderRadius="6px" animation="wave" />
          </div>
        </template>

        <template #content>
          <div class="mt-2 flex flex-col gap-2 text-sm">
            <div v-for="i in 4" :key="i" class="flex items-center justify-between">
              <Skeleton width="4rem" height="0.9rem" animation="wave" />
              <Skeleton width="5rem" height="0.9rem" animation="wave" />
            </div>
          </div>
        </template>

        <template #footer>
          <div class="mt-2 flex flex-wrap gap-2">
            <Skeleton
              v-for="i in 5"
              :key="i"
              width="2rem"
              height="2rem"
              borderRadius="50%"
              animation="wave"
            />
          </div>
        </template>
      </Card>
    </div>

    <!-- CONTENT -->
    <div
      v-else-if="visibleUsers.length"
      class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
    >
      <div
        v-for="u in visibleUsers"
        :key="u.id"
        class="relative flex flex-col justify-between overflow-hidden rounded-2xl border bg-surface-0 shadow-sm transition-all duration-200 dark:bg-surface-900"
        :class="[
          u.isDeleted
            ? 'bg-pink-50 opacity-90 ring-1 ring-pink-200/50 dark:border-pink-100 dark:bg-pink-100/10'
            : 'border-surface-200 bg-surface-0 hover:shadow-primary/40 dark:border-surface-700 dark:bg-surface-900',
        ]"
      >
        <!-- Header -->
        <div class="flex items-center justify-between p-4">
          <div class="flex items-center gap-3">
            <Avatar
              :label="u.fullName?.charAt(0)"
              size="large"
              shape="circle"
              class="text-white shadow-sm"
              :class="[
                u.isActive ? 'bg-green-500' : 'bg-gray-500',
                u.isDeleted ? 'opacity-60 grayscale' : '',
              ]"
            />
            <div>
              <h4 class="text-900 dark:text-0 m-0 text-base font-semibold">
                {{ u.fullName }}
              </h4>
              <p class="m-0 truncate text-xs text-surface-500 dark:text-surface-400">
                {{ u.email }}
              </p>
            </div>
          </div>
          <Tag v-if="u.isDeleted" value="Deleted" severity="danger" rounded class="ml-2" />
          <div class="flex items-center gap-2">
            <Checkbox v-if="!u.isDeleted" :value="u.id" v-model="selectedUserIds" />
          </div>
        </div>

        <!-- Content -->
        <div class="border-t border-surface-200 px-4 pb-3 pt-2 text-sm dark:border-surface-700">
          <!-- Status -->
          <div class="flex items-center justify-between py-1">
            <span class="flex items-center gap-1 text-surface-600 dark:text-surface-400">
              <i class="pi pi-check-circle text-surface-500"></i>
              Status
            </span>
            <Tag
              :value="u.isActive ? 'Active' : 'Inactive'"
              :severity="u.isActive ? 'success' : 'danger'"
              rounded
              class="text-xs font-semibold"
            />
          </div>
          <div class="flex items-center justify-between py-1">
            <span class="flex items-center gap-1 text-surface-600 dark:text-surface-400">
              <i class="pi pi-id-card text-surface-500"></i>
              Role
            </span>
            <Tag
              :value="u.roleName"
              :severity="u.isSystemRole ? 'danger' : 'info'"
              rounded
              class="px-2 py-0.5 text-xs font-medium"
            />
          </div>

          <div class="flex items-center justify-between py-1">
            <span class="flex items-center gap-1 text-surface-600 dark:text-surface-400">
              <i class="pi pi-building text-surface-500"></i>
              Clinic
            </span>
            <span class="truncate text-surface-700 dark:text-surface-200">
              {{ u.clinicName || 'No Clinic' }}
            </span>
          </div>

          <!-- Static Details Section -->
          <div
            class="mt-3 space-y-2 rounded-xl border border-surface-200 bg-surface-50 px-3 py-2 text-xs text-surface-700 dark:border-surface-700 dark:bg-surface-800 dark:text-surface-300"
          >
            <div class="flex items-center justify-between">
              <span class="flex items-center gap-1 font-medium">
                <i class="pi pi-user text-primary"></i> Created By
              </span>
              <span>{{ u.createdByUserName || 'Unknown' }}</span>
            </div>

            <div class="flex items-center justify-between">
              <span class="flex items-center gap-1 font-medium">
                <i class="pi pi-calendar text-green-500"></i> Created
              </span>
              <span>{{ formatDate(u.createdAt) }}</span>
            </div>

            <div class="flex items-center justify-between">
              <span class="flex items-center gap-1 font-medium">
                <i class="pi pi-refresh text-cyan-500"></i> Updated
              </span>
              <span>{{ formatDate(u.updatedAt) }}</span>
            </div>
          </div>
        </div>

        <!-- Footer (Actions) -->
        <div
          class="flex items-center justify-between border-t border-surface-200 bg-surface-50 px-4 py-2 dark:border-surface-700 dark:bg-surface-800"
        >
          <div class="font-mono text-xs text-surface-500 dark:text-surface-400">
            ID: {{ String(u.id).slice(0, 8) }}
          </div>

          <div class="flex gap-1">
            <!-- ✅ When NOT DELETED -->
            <template v-if="!u.isDeleted">
              <Button
                v-if="canEdit"
                icon="pi pi-pencil"
                text
                size="small"
                v-tooltip.top="'Edit user'"
                @click="editUser(u)"
              />
              <Button
                v-if="canActivate"
                :icon="u.isActive ? 'pi pi-ban' : 'pi pi-check'"
                text
                size="small"
                v-tooltip.top="u.isActive ? 'Deactivate' : 'Activate'"
                @click="toggleUserStatus(u)"
                :loading="loadingUserIds.has(u.id)"
                :disabled="loadingUserIds.has(u.id)"
              />
              <Button
                v-if="canResetPassword"
                icon="pi pi-key"
                text
                size="small"
                v-tooltip.top="'Reset password'"
                @click="openChangePasswordDialog(u)"
              />
              <Button
                v-if="canDelete"
                icon="pi pi-trash"
                text
                size="small"
                severity="danger"
                v-tooltip.top="'Soft delete'"
                @click="confirmDeleteUser(u)"
              />
            </template>

            <!-- ✅ When DELETED -->
            <template v-else>
              <Button
                icon="pi pi-undo"
                text
                size="small"
                severity="success"
                v-tooltip.top="'Restore user'"
                @click="confirmRestore(u)"
              />

              <Button
                icon="pi pi-trash"
                text
                size="small"
                severity="danger"
                v-tooltip.top="'Permanently delete'"
                @click="hardDeleteUser(u)"
              />
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- EMPTY STATE -->
    <EmptyState
      v-else
      icon="pi pi-users"
      title="No Users Found"
      description="No users match your search or filters"
    />

    <!-- INFINITE SCROLL -->
    <div ref="sentinel" class="h-6"></div>
    <div v-if="loadingMore" class="flex justify-center p-4">
      <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
    </div>

    <!-- FILTERS DIALOG -->
    <Dialog
      v-model:visible="filtersDialogVisible"
      header="Filters"
      modal
      :style="{ width: '500px', maxWidth: '95vw' }"
    >
      <div class="grid gap-4">
        <div>
          <label class="mb-2 block text-sm font-medium">Full Name</label>
          <InputText
            v-model="filters.fullName.constraints[0].value"
            placeholder="Search by full name..."
            class="w-full"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium">Email</label>
          <InputText
            v-model="filters.email.constraints[0].value"
            placeholder="Search by email..."
            class="w-full"
          />
        </div>

        <!-- ✅ Replaced with RoleSelect -->
        <div>
          <label class="mb-2 block text-sm font-medium">Role</label>
          <RoleSelect
            v-model="filters.roleName.constraints[0].value"
            placeholder="Select a role..."
            :showLabel="false"
            clearable
            valueKey="name"
          />
        </div>

        <!-- ✅ Replaced with ClinicSelect -->
        <div>
          <label class="mb-2 block text-sm font-medium">Clinic</label>
          <ClinicSelect
            v-model="filters.clinicName.constraints[0].value"
            placeholder="Select a clinic..."
            :showLabel="false"
            valueKey="name"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium">Status</label>
          <Select
            v-model="filters.isActive.constraints[0].value"
            :options="statusOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Select status..."
            showClear
            class="w-full"
          >
            <template #value="{ value }">
              <div v-if="value !== null && value !== undefined" class="flex items-center gap-2">
                <Tag
                  :value="value ? 'Active' : 'Inactive'"
                  :severity="value ? 'success' : 'danger'"
                />
              </div>
              <span v-else class="text-surface-500">Select status</span>
            </template>

            <template #option="{ option }">
              <Tag :value="option.label" :severity="option.severity" />
            </template>
          </Select>
        </div>

        <!-- here -->
        <div>
          <label class="inline-flex items-center gap-2">
            <Checkbox v-model="showDeleted" :binary="true" />
            <span>Show deleted users</span>
          </label>
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium">Created Date</label>
          <DatePicker
            v-model="filters.createdAt.constraints[0].value"
            dateFormat="mm/dd/yy"
            placeholder="Select date..."
            showIcon
            class="w-full"
          />
        </div>
      </div>

      <template #footer>
        <div class="flex justify-between gap-2">
          <Button
            icon="pi pi-filter-slash"
            label="Clear All"
            outlined
            severity="secondary"
            @click="clearFilters"
          />
          <Button icon="pi pi-check" label="Apply Filters" @click="applyFilters" />
        </div>
      </template>
    </Dialog>

    <!-- DIALOGS -->
    <UserDialog v-model:visible="userDialog" :user="user" @save="saveUser" @cancel="hideDialog" />

    <ChangePasswordDialog
      v-model:visible="changePasswordDialog"
      :user="user"
      @save="handleChangePassword"
    />

    <!-- SOFT DELETE DIALOG -->
    <Dialog
      v-model:visible="deleteUserDialog"
      header="Confirm Delete"
      modal
      :style="{ width: '450px' }"
    >
      <p>
        Are you sure you want to delete <b>{{ user.fullName }}</b
        >? This user will be soft-deleted and can be recovered.
      </p>
      <template #footer>
        <Button
          label="Cancel"
          outlined
          @click="deleteUserDialog = false"
          :disabled="softDeleteLoading"
        />
        <Button
          label="Delete"
          severity="danger"
          @click="deleteUser"
          :loading="softDeleteLoading"
          :disabled="softDeleteLoading"
        />
      </template>
    </Dialog>

    <Dialog
      v-model:visible="bulkActionDialog"
      :header="bulkDialogData.header"
      modal
      :style="{ width: '450px' }"
    >
      <p>{{ bulkDialogData.message }}</p>
      <template #footer>
        <Button
          label="Cancel"
          outlined
          @click="bulkActionDialog = false"
          :disabled="bulkActionLoading"
        />
        <Button
          :label="bulkDialogData.actionLabel"
          :icon="bulkDialogData.actionIcon"
          :severity="bulkDialogData.severity"
          @click="handleBulkActionConfirm"
          :loading="bulkActionLoading"
          :disabled="bulkActionLoading"
        />
      </template>
    </Dialog>

    <ConfirmDialog />
  </div>
</template>
