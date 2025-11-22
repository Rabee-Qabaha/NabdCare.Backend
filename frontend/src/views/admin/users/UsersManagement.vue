<script setup lang="ts">
  import EmptyState from '@/components/EmptyState.vue';
  import ResetPasswordDialog from '@/components/User/ResetPasswordDialog.vue';
  import UserDialog from '@/components/User/UserDialog.vue';
  import { useClinics, useRoles } from '@/composables/query/useDropdownData';
  import {
    useActivateUser,
    useCreateUser,
    useDeactivateUser,
    useRestoreUser,
    useSoftDeleteUser,
    useUpdateUser,
  } from '@/composables/query/users/useUserActions';

  import { useInfiniteUsersPaged } from '@/composables/query/users/useUsers';
  import { usePermission } from '@/composables/usePermission';
  import { useUserDialog, useUserFilters, useUserUIActions } from '@/composables/user';
  import PageHeader from '@/layout/PageHeader.vue';
  import type { PaginatedResult, UserResponseDto } from '@/types/backend';
  import { formatUserDisplay } from '@/utils/users/userDisplay';
  import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';
  import RoleSelect from '@/components/Dropdowns/RoleSelect.vue';
  import { useToastService } from '@/composables/useToastService';
  import type { InfiniteData } from '@tanstack/vue-query';
  import Avatar from 'primevue/avatar';
  import Button from 'primevue/button';
  import Card from 'primevue/card';
  import Checkbox from 'primevue/checkbox';
  import ConfirmDialog from 'primevue/confirmdialog';
  import DatePicker from 'primevue/datepicker';
  import Dialog from 'primevue/dialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import ProgressSpinner from 'primevue/progressspinner';
  import Select from 'primevue/select';
  import Skeleton from 'primevue/skeleton';
  import Tag from 'primevue/tag';

  const toast = useToastService();
  const { can: canPermission } = usePermission();

  useRoles();
  useClinics();

  const BATCH_SIZE = 20;
  const visibleCount = ref(BATCH_SIZE);
  const loadingMore = ref(false);
  const sentinel = ref<HTMLElement | null>(null);
  let observer: IntersectionObserver | null = null;
  const selectedUserIds = ref<string[]>([]);
  const bulkActionLoading = ref(false);
  const currentBulkAction = ref<'activate' | 'deactivate' | 'delete' | null>(null);
  const bulkActionDialog = ref(false);
  const filtersDialogVisible = ref(false);
  const resetPasswordDialog = ref(false);

  const canCreate = computed(() => canPermission('Users.Create'));
  const canEdit = computed(() => canPermission('Users.Edit'));
  const canDelete = computed(() => canPermission('Users.Delete'));
  const canResetPassword = computed(() => canPermission('Users.ResetPassword'));
  const canActivate = computed(() => canPermission('Users.Activate'));

  const showDeleted = ref(false);

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

  const allUsersRef = computed<UserResponseDto[]>(() => {
    const data = infiniteData.value as InfiniteData<PaginatedResult<UserResponseDto>> | undefined;

    if (!data?.pages) return [];

    return data.pages.flatMap((page: PaginatedResult<UserResponseDto>) => page.items ?? []);
  });

  const { filters, filteredUsers, resetFilters, clearFilter } = useUserFilters(allUsersRef);

  const {
    visible: userDialogVisible,
    localUser,
    open: openUserDialog,
    close: closeUserDialog,
  } = useUserDialog();

  const { loadingUserIds, toggleUserStatus, confirmSoftDelete, confirmHardDelete, confirmRestore } =
    useUserUIActions();

  const createUserMutation = useCreateUser();
  const updateUserMutation = useUpdateUser();
  const softDeleteMutation = useSoftDeleteUser();
  const restoreMutation = useRestoreUser();
  const activateUserMutation = useActivateUser();
  const deactivateUserMutation = useDeactivateUser();

  const visibleUsers = computed(() => {
    // Always apply filters
    const list = filteredUsers.value;

    // If showDeleted is false → hide deleted users
    const finalList = showDeleted.value ? list : list.filter((u) => !u.isDeleted);

    return finalList.slice(0, visibleCount.value);
  });
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

  const userPermissionsDialogVisible = ref(false);
  const selectedUserForPermissions = ref<UserResponseDto | null>(null);

  function openUserPermissionsDialog(user: UserResponseDto) {
    selectedUserForPermissions.value = user;
    userPermissionsDialogVisible.value = true;
  }

  function toggleShowDeleted() {
    showDeleted.value = !showDeleted.value;
    visibleCount.value = BATCH_SIZE;
    refetch();
  }

  function openNew(): void {
    openUserDialog();
  }

  function editUser(u: UserResponseDto): void {
    openUserDialog(u);
  }

  function openFilters(): void {
    filtersDialogVisible.value = true;
  }

  function applyFilters(): void {
    filtersDialogVisible.value = false;
    visibleCount.value = BATCH_SIZE;
    refetch();
  }

  function openResetPasswordDialog(u: UserResponseDto): void {
    localUser.value = { ...u };
    resetPasswordDialog.value = true;
  }

  async function saveUser(newUser: any): Promise<void> {
    try {
      if (!newUser.id) {
        await createUserMutation.mutateAsync(newUser);
        toast.success(`User ${newUser.email} created`);
      } else {
        await updateUserMutation.mutateAsync({
          id: newUser.id,
          data: newUser,
        });
        toast.success('User updated');
      }
      closeUserDialog();
    } catch (err: any) {
      toast.error(err.message || 'Failed to save user');
    }
  }

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
      for (const id of ids) {
        try {
          if (action === 'activate') {
            await activateUserMutation.mutateAsync(id);
            successCount++;
          } else if (action === 'deactivate') {
            await deactivateUserMutation.mutateAsync(id);
            successCount++;
          } else if (action === 'delete') {
            await softDeleteMutation.mutateAsync(id);
            successCount++;
          }
        } catch (err) {
          errorCount++;
          console.error(`Failed to ${action} user ${id}:`, err);
        }
      }

      if (successCount > 0) {
        const actionText = {
          activate: 'activated',
          deactivate: 'deactivated',
          delete: 'deleted',
        }[action];

        toast.success(
          errorCount === 0
            ? `${successCount} user${successCount !== 1 ? 's' : ''} ${actionText}`
            : `${successCount} user${successCount !== 1 ? 's' : ''} ${actionText}, ${errorCount} failed`,
        );
      }

      if (errorCount > 0) {
        toast.warn(`${errorCount} user${errorCount !== 1 ? 's' : ''} could not be ${action}d`);
      }

      selectedUserIds.value = [];
      await refetch();
    } catch (err: any) {
      toast.error(err.message || 'Bulk action failed');
    } finally {
      bulkActionLoading.value = false;
      currentBulkAction.value = null;
    }
  }

  function setupObserver(): void {
    if (observer) observer.disconnect();
    if (!sentinel.value) return;

    observer = new IntersectionObserver(async (entries) => {
      const [entry] = entries;
      if (!entry!.isIntersecting) return;

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
  <div className="card">
    <div class="space-y-4 p-4 md:p-6">
      <PageHeader
        title="Users Management"
        description="Manage all users across all clinics"
        :stats="[{ icon: 'pi pi-users', label: `${visibleUsers.length} Total Users` }]"
      ></PageHeader>

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
          <Button
            :label="showDeleted ? 'Show Active' : 'Show Deleted'"
            :icon="showDeleted ? 'pi pi-users' : 'pi pi-trash'"
            outlined
            @click="toggleShowDeleted"
          />
          <Button icon="pi pi-filter-slash" label="Clear Filters" outlined @click="resetFilters" />
          <Button
            icon="pi pi-refresh"
            label="Refresh"
            outlined
            @click="refetch()"
            :loading="isFetching"
          />
        </div>
      </div>

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
          {{ filteredUsers.length }} result{{ filteredUsers.length !== 1 ? 's' : '' }} found
        </small>
      </div>

      <div v-if="isError" class="rounded-lg bg-red-50 p-4 dark:bg-red-900">
        <p class="text-red-800 dark:text-red-200">
          ❌ {{ error?.message || 'Failed to load users' }}
        </p>
        <Button label="Retry" icon="pi pi-refresh" text @click="refetch()" class="mt-2" />
      </div>

      <div
        v-if="isFetching && allUsersRef.length === 0"
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

      <div
        v-else-if="visibleUsers.length"
        class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
      >
        <div
          v-for="u in visibleUsers"
          :key="u.id"
          class="relative flex flex-col justify-between overflow-hidden rounded-2xl border shadow-sm transition-all duration-200"
          :class="[
            u.isDeleted
              ? 'border-pink-200 bg-pink-50 dark:border-pink-400 dark:bg-pink-550'
              : 'border-surface-200 bg-surface-0 dark:border-surface-700 dark:bg-surface-900',
            'hover:shadow-primary/40',
          ]"
        >
          <div class="flex items-center justify-between p-4">
            <div class="flex items-center gap-3">
              <Avatar
                :label="formatUserDisplay(u).initials"
                size="large"
                shape="circle"
                class="text-white shadow-sm"
                :class="
                  u.isDeleted ? 'opacity-60 grayscale' : u.isActive ? 'bg-green-500' : 'bg-gray-500'
                "
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

          <div
            class="border-t px-4 pb-3 pt-2 text-sm"
            :class="
              u.isDeleted
                ? 'border-pink-200 dark:border-pink-700'
                : 'border-surface-200 dark:border-surface-700'
            "
          >
            <div class="flex items-center justify-between py-1">
              <span class="flex items-center gap-1 text-surface-600 dark:text-surface-400">
                <i class="pi pi-check-circle text-surface-500"></i>
                Status
              </span>
              <Tag
                :value="formatUserDisplay(u).statusBadge.value"
                :severity="formatUserDisplay(u).statusBadge.severity"
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
                :severity="formatUserDisplay(u).roleTagSeverity"
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

            <div
              class="mt-3 space-y-2 rounded-xl border px-3 py-2 text-xs"
              :class="
                u.isDeleted
                  ? 'border-pink-200 bg-pink-100/50 text-pink-800 dark:border-pink-700 dark:bg-pink-900/30 dark:text-pink-200'
                  : 'border-surface-200 bg-surface-50 text-surface-700 dark:border-surface-700 dark:bg-surface-800 dark:text-surface-300'
              "
            >
              <div class="flex items-center justify-between">
                <span class="flex items-center gap-1 font-medium">
                  <i class="pi pi-user text-primary"></i>
                  Created By
                </span>
                <span>{{ u.createdByUserName || 'Unknown' }}</span>
              </div>

              <div class="flex items-center justify-between">
                <span class="flex items-center gap-1 font-medium">
                  <i class="pi pi-calendar text-green-500"></i>
                  Created
                </span>
                <!-- <span>{{ formatDate(u.createdAt) }}</span> -->
                <span>{{ u.createdAt }}</span>
              </div>

              <div class="flex items-center justify-between">
                <span class="flex items-center gap-1 font-medium">
                  <i class="pi pi-refresh text-cyan-500"></i>
                  Updated
                </span>
                <span>{{ u.updatedAt }}</span>
              </div>
            </div>
          </div>

          <div
            class="flex items-center justify-between border-t px-4 py-2"
            :class="
              u.isDeleted
                ? 'border-pink-200 bg-pink-100/30 dark:border-pink-700 dark:bg-pink-900/20'
                : 'border-surface-200 bg-surface-50 dark:border-surface-700 dark:bg-surface-800'
            "
          >
            <div class="font-mono text-xs text-surface-500 dark:text-surface-400">
              ID: {{ String(u.id).slice(0, 8) }}
            </div>

            <div class="flex gap-1">
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
                  @click="openResetPasswordDialog(u)"
                />
                <Button
                  v-if="canDelete"
                  icon="pi pi-trash"
                  text
                  size="small"
                  severity="danger"
                  v-tooltip.top="'Delete'"
                  @click="confirmSoftDelete(u, () => softDeleteMutation.mutateAsync(u.id))"
                />
                <Button
                  icon="pi pi-shield"
                  text
                  size="small"
                  v-tooltip.top="'Manage Permissions'"
                  @click="openUserPermissionsDialog(u)"
                />
              </template>

              <template v-else>
                <Button
                  icon="pi pi-undo"
                  text
                  size="small"
                  severity="success"
                  v-tooltip.top="'Restore user'"
                  @click="confirmRestore(u, () => restoreMutation.mutateAsync(u.id))"
                />

                <Button
                  icon="pi pi-trash"
                  text
                  size="small"
                  severity="danger"
                  v-tooltip.top="'Permanently delete'"
                  @click="confirmHardDelete(u, () => {})"
                />
              </template>
            </div>
          </div>
        </div>
      </div>

      <EmptyState
        v-else
        icon="pi pi-users"
        title="No Users Found"
        description="No users match your search or filters"
      />

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
        class="rounded-xl p-3"
      >
        <div class="flex flex-col gap-6 p-3">
          <!-- Full Name -->
          <FloatLabel variant="on">
            <InputText
              id="filterFullName"
              v-model="filters.fullName.constraints[0].value"
              class="w-full"
            />
            <label for="filterFullName">Full Name</label>
          </FloatLabel>

          <!-- Email -->
          <FloatLabel variant="on">
            <InputText
              id="filterEmail"
              v-model="filters.email.constraints[0].value"
              class="w-full"
            />
            <label for="filterEmail">Email</label>
          </FloatLabel>

          <!-- Role (NO FloatLabel needed) -->
          <RoleSelect
            v-model="filters.roleName.constraints[0].value"
            showLabel
            label="Select a Role"
            :showClear="true"
            valueKey="name"
          />

          <!-- Clinic (NO FloatLabel needed) -->
          <ClinicSelect
            v-model="filters.clinicName.constraints[0].value"
            showLabel
            label="Select a Clinic"
            :showClear="true"
            valueKey="name"
          />

          <!-- Status -->
          <FloatLabel variant="on">
            <Select
              id="filterStatus"
              v-model="filters.isActive.constraints[0].value"
              :options="[
                { label: 'Active', value: true, severity: 'success' },
                { label: 'Inactive', value: false, severity: 'danger' },
              ]"
              optionLabel="label"
              optionValue="value"
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
              </template>
            </Select>
            <label>Status</label>
          </FloatLabel>

          <!-- Created Date -->
          <FloatLabel variant="on">
            <DatePicker
              id="filterDate"
              v-model="filters.createdAt.constraints[0].value"
              dateFormat="mm/dd/yy"
              showIcon
              style="width: 100%"
              inputClass="w-full"
            />
            <label for="filterDate">Created Date</label>
          </FloatLabel>
        </div>

        <template #footer>
          <div class="flex justify-between gap-2">
            <Button
              icon="pi pi-filter-slash"
              label="Clear All"
              outlined
              severity="secondary"
              @click="
                () => {
                  resetFilters();
                  filtersDialogVisible = false;
                }
              "
            />
            <Button icon="pi pi-check" label="Apply Filters" @click="applyFilters" />
          </div>
        </template>
      </Dialog>

      <!-- BULK ACTION DIALOG -->
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

      <!-- USER DIALOG -->
      <UserDialog v-model:visible="userDialogVisible" :user="localUser" @save="saveUser" />

      <!-- CHANGE PASSWORD DIALOG -->
      <ResetPasswordDialog
        v-model:visible="resetPasswordDialog"
        :userId="localUser?.id || null"
        :fullName="localUser?.fullName"
        :email="localUser?.email"
        @success="() => toast.success('Password reset successfully')"
      />

      <UserPermissionsDialog
        v-model:visible="userPermissionsDialogVisible"
        :userId="selectedUserForPermissions?.id || null"
        :fullName="selectedUserForPermissions?.fullName"
        :email="selectedUserForPermissions?.email"
      />
      <ConfirmDialog />
    </div>
  </div>
</template>
