// src/views/admin/users/UsersManagement.vue
<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <header>
        <PageHeader
          title="Users Management"
          description="Manage all users across all clinics"
          :stats="[{ icon: 'pi pi-users', label: `${filteredUsers.length} Total Users` }]"
        />
      </header>

      <UserToolbar
        :loading="isFetching"
        :include-deleted="includeDeleted"
        :can-create="canCreate"
        :can-activate="canActivate"
        :can-delete="canDelete"
        :has-selection="selectedUserIds.length > 0"
        @create="openCreateDialog"
        @filters="filtersDialogVisible = true"
        @toggle-deleted="toggleDeletedView"
        @reset-filters="resetAllFilters"
        @refresh="refetch"
        @bulk-activate="() => openBulkActionDialog('activate')"
        @bulk-deactivate="() => openBulkActionDialog('deactivate')"
        @bulk-delete="() => openBulkActionDialog('delete')"
      />

      <div>
        <IconField class="w-full">
          <InputIcon><i class="pi pi-search" /></InputIcon>
          <InputText
            v-model="activeFilters.global"
            placeholder="Search users by name, email, role, or clinic..."
            class="w-full"
          />
        </IconField>
        <small v-if="activeFilters.global && !showGridLoading" class="mt-1 block text-surface-500">
          {{ filteredUsers.length }} result{{ filteredUsers.length !== 1 ? 's' : '' }} found
        </small>
      </div>

      <div
        v-if="error"
        class="rounded-lg bg-red-50 p-4 dark:bg-red-900/20 border border-red-200 dark:border-red-800"
      >
        <p class="text-sm text-red-700 dark:text-red-300">{{ errorMessage }}</p>
      </div>

      <div
        v-if="showGridLoading"
        class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
      >
        <UserCardSkeleton v-for="n in 8" :key="n" />
      </div>

      <div
        v-else-if="visibleUsers.length > 0"
        class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
      >
        <UserCard
          v-for="u in visibleUsers"
          :key="u.id"
          :user="u"
          v-model:selected="selectedUserIds"
        >
          <template #actions>
            <div class="flex gap-1">
              <template v-if="!u.isDeleted">
                <Button
                  v-if="canEdit"
                  icon="pi pi-pencil"
                  text
                  size="small"
                  severity="info"
                  v-tooltip.top="'Edit'"
                  @click="openEditDialog(u)"
                />
                <Button
                  v-if="canActivate"
                  :icon="u.isActive ? 'pi pi-ban' : 'pi pi-check'"
                  text
                  size="small"
                  :severity="u.isActive ? 'warn' : 'success'"
                  v-tooltip.top="u.isActive ? 'Deactivate' : 'Activate'"
                  @click="toggleUserStatus(u)"
                  :loading="loadingUserIds.has(u.id)"
                />
                <Button
                  v-if="canResetPassword"
                  icon="pi pi-key"
                  text
                  size="small"
                  severity="help"
                  v-tooltip.top="'Reset Password'"
                  @click="openResetPasswordDialog(u)"
                />
                <Button
                  icon="pi pi-shield"
                  text
                  size="small"
                  severity="primary"
                  v-tooltip.top="'Permissions'"
                  @click="openPermissionsDialog(u)"
                />
                <Button
                  v-if="canDelete"
                  icon="pi pi-trash"
                  text
                  size="small"
                  severity="danger"
                  v-tooltip.top="'Move to Trash'"
                  @click="openDeleteDialog(u)"
                />
              </template>

              <template v-else>
                <Button
                  v-if="canDelete"
                  icon="pi pi-undo"
                  text
                  size="small"
                  severity="success"
                  v-tooltip.top="'Restore'"
                  @click="restoreUser(u)"
                />
                <Button
                  v-if="canDelete"
                  icon="pi pi-times-circle"
                  text
                  size="small"
                  severity="danger"
                  v-tooltip.top="'Permanently Delete'"
                  @click="openPermanentDeleteDialog(u)"
                />
              </template>
            </div>
          </template>
        </UserCard>
      </div>

      <EmptyState
        v-else
        icon="pi pi-users"
        title="No Users Found"
        description="No users match your search or filters"
      />

      <div ref="sentinel" class="h-6"></div>
      <div v-if="isFetchingNextPage" class="flex justify-center p-4">
        <ProgressSpinner style="width: 30px; height: 30px" strokeWidth="4" />
      </div>

      <UserFilters
        v-model:visible="filtersDialogVisible"
        :filters="activeFilters"
        @apply="applyFilters"
        @reset="resetAllFilters"
      />

      <UserDialog v-model:visible="dialogs.createEdit" :user="selectedUser" @save="saveUser" />

      <ResetPasswordDialog
        v-model:visible="dialogs.resetPassword"
        :user-id="selectedUser?.id || null"
        :full-name="selectedUser?.fullName"
        :email="selectedUser?.email"
      />

      <UserPermissionsDialog
        v-model:visible="dialogs.permissions"
        :user-id="selectedUser?.id || null"
      />

      <DeleteConfirmDialog
        v-model:visible="dialogs.deleteConfirm"
        mode="soft"
        title="Move to Trash?"
        :message="`Are you sure you want to move this user to the trash? They will lose access immediately.`"
        :item-identifier="selectedUser?.fullName"
        :loading="isDeleting"
        @confirm="softDeleteUser"
      />

      <DeleteConfirmDialog
        v-model:visible="dialogs.permanentDelete"
        mode="hard"
        :item-identifier="selectedUser?.fullName"
        :loading="isDeleting"
        @confirm="hardDeleteUser"
      />

      <Dialog
        v-model:visible="dialogs.bulkAction"
        :header="bulkHeader"
        modal
        :style="{ width: '400px' }"
      >
        <p>
          Are you sure you want to
          <b>{{ currentBulkAction }}</b>
          {{ selectedUserIds.length }} users?
        </p>
        <template #footer>
          <Button label="Cancel" text severity="secondary" @click="dialogs.bulkAction = false" />
          <Button
            label="Confirm"
            :severity="currentBulkAction === 'delete' ? 'danger' : 'primary'"
            :loading="bulkActionLoading"
            @click="handleBulkAction"
          />
        </template>
      </Dialog>

      <ConfirmDialog />
    </div>
  </div>
</template>

<script setup lang="ts">
  // Components
  import EmptyState from '@/components/EmptyState.vue';
  import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue';
  import ResetPasswordDialog from '@/components/User/ResetPasswordDialog.vue';
  import UserCard from '@/components/User/UserCard.vue';

  // ✅ Import the new Skeleton
  import UserCardSkeleton from '@/components/User/UserCardSkeleton.vue';

  import UserDialog from '@/components/User/UserDialog.vue';
  import UserFilters from '@/components/User/UserFilters.vue';
  import UserPermissionsDialog from '@/components/User/userPermissionsDialog.vue';
  import UserToolbar from '@/components/User/UserToolbar.vue';
  import PageHeader from '@/layout/PageHeader.vue';

  // PrimeVue
  import Button from 'primevue/button';
  import ConfirmDialog from 'primevue/confirmdialog';
  import Dialog from 'primevue/dialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import ProgressSpinner from 'primevue/progressspinner';
  // Removed Skeleton from here as it's now inside UserCardSkeleton

  import { useUserActions } from '@/composables/query/users/useUserActions';
  import { useInfiniteUsersPaged } from '@/composables/query/users/useUsers';
  import { usePermission } from '@/composables/usePermission';
  import { useUserDialogs } from '@/composables/user/useUserDialog';
  import { useUserFilters } from '@/composables/user/useUserFilters';
  import type { PaginatedResult, UserResponseDto } from '@/types/backend';
  import type { InfiniteData } from '@tanstack/vue-query';
  import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

  const { can } = usePermission();

  // Permissions
  const canCreate = computed(() => can('Users.Create'));
  const canEdit = computed(() => can('Users.Edit'));
  const canDelete = computed(() => can('Users.Delete'));
  const canResetPassword = computed(() => can('Users.ResetPassword'));
  const canActivate = computed(() => can('Users.Activate'));

  // 1. Data Fetching
  const includeDeleted = ref(false);

  const {
    data: infiniteData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isFetching, // This is true when refetching OR loading next page
    isLoading, // This is true only on initial load
    refetch,
    error,
  } = useInfiniteUsersPaged({
    search: '',
    includeDeleted: includeDeleted,
  });

  const allUsersRef = computed<UserResponseDto[]>(() => {
    const data = infiniteData.value as unknown as InfiniteData<PaginatedResult<UserResponseDto>>;
    if (!data?.pages) return [];
    return data.pages.flatMap((page) => page.items ?? []);
  });

  const showGridLoading = computed(() => {
    if (isLoading.value) return true;
    if (isFetching.value && visibleUsers.value.length === 0) return true;
    return false;
  });

  const errorMessage = computed(() => (error.value ? (error.value as any).message : null));

  // 2. Filtering & Logic
  const {
    activeFilters,
    filteredUsers,
    resetFilters: resetLocalFilters,
  } = useUserFilters(allUsersRef);
  const visibleCount = ref(20);
  const visibleUsers = computed(() => filteredUsers.value.slice(0, visibleCount.value));

  // 3. Dialog State
  const {
    dialogs,
    selectedUser,
    currentBulkAction,
    closeAll,
    openCreateDialog,
    openEditDialog,
    openPermissionsDialog,
    openResetPasswordDialog,
    openDeleteDialog,
    openPermanentDeleteDialog,
    openBulkActionDialog,
  } = useUserDialogs();

  const filtersDialogVisible = ref(false);
  const selectedUserIds = ref<string[]>([]);
  const isDeleting = ref(false);

  // 4. Actions
  const {
    createUserMutation,
    updateUserMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
    activateMutation,
    deactivateMutation,
  } = useUserActions();

  const loadingUserIds = ref(new Set<string>());

  // 5. Handlers

  function applyFilters(newFilters: any) {
    Object.assign(activeFilters, newFilters);
    const needsDeleted = newFilters.status === 'deleted' || newFilters.status === 'all';
    if (needsDeleted && !includeDeleted.value) {
      includeDeleted.value = true;
    } else if (!needsDeleted && includeDeleted.value) {
      includeDeleted.value = false;
    }
  }

  function resetAllFilters() {
    resetLocalFilters();
    includeDeleted.value = false;
  }

  function toggleDeletedView() {
    if (activeFilters.status === 'active') {
      activeFilters.status = 'deleted';
      includeDeleted.value = true;
    } else {
      activeFilters.status = 'active';
      includeDeleted.value = false;
    }
  }

  async function saveUser(userData: any) {
    try {
      if (userData.id) {
        await updateUserMutation.mutateAsync({ id: userData.id, data: userData });
      } else {
        await createUserMutation.mutateAsync(userData);
      }
      closeAll();
    } catch (e: any) {}
  }

  async function toggleUserStatus(user: UserResponseDto) {
    loadingUserIds.value.add(user.id);
    try {
      if (user.isActive) await deactivateMutation.mutateAsync(user.id);
      else await activateMutation.mutateAsync(user.id);
      refetch();
    } finally {
      loadingUserIds.value.delete(user.id);
    }
  }

  function softDeleteUser() {
    if (!selectedUser.value) return;
    isDeleting.value = true;
    softDeleteMutation.mutate(selectedUser.value.id, {
      onSuccess: () => {
        closeAll();
        refetch();
        isDeleting.value = false;
      },
      onError: () => {
        isDeleting.value = false;
      },
    });
  }

  function hardDeleteUser() {
    if (!selectedUser.value) return;
    isDeleting.value = true;
    hardDeleteMutation.mutate(selectedUser.value.id, {
      onSuccess: () => {
        closeAll();
        refetch();
        isDeleting.value = false;
      },
      onError: () => {
        isDeleting.value = false;
      },
    });
  }

  function restoreUser(user: UserResponseDto) {
    restoreMutation.mutate(user.id, {
      onSuccess: () => {
        refetch();
      },
    });
  }

  // --- Bulk Actions ---
  const bulkActionLoading = ref(false);
  const bulkHeader = computed(() =>
    currentBulkAction.value === 'activate'
      ? 'Bulk Activate'
      : currentBulkAction.value === 'deactivate'
        ? 'Bulk Deactivate'
        : 'Bulk Delete',
  );

  async function handleBulkAction() {
    if (!selectedUserIds.value.length || !currentBulkAction.value) return;

    bulkActionLoading.value = true;
    try {
      const promises = selectedUserIds.value.map((id) => {
        if (currentBulkAction.value === 'activate') return activateMutation.mutateAsync(id);
        if (currentBulkAction.value === 'deactivate') return deactivateMutation.mutateAsync(id);
        if (currentBulkAction.value === 'delete') return softDeleteMutation.mutateAsync(id);
      });
      await Promise.allSettled(promises);
      selectedUserIds.value = [];
      closeAll();
      refetch();
    } finally {
      bulkActionLoading.value = false;
    }
  }

  // --- Infinite Scroll ---
  const sentinel = ref<HTMLElement | null>(null);
  let observer: IntersectionObserver;

  function setupObserver() {
    if (observer) observer.disconnect();
    observer = new IntersectionObserver(async (entries) => {
      const entry = entries[0];
      if (entry && entry.isIntersecting && hasNextPage.value && !isFetchingNextPage.value) {
        await fetchNextPage();
        visibleCount.value += 20;
      }
    });
    if (sentinel.value) observer.observe(sentinel.value);
  }

  onMounted(setupObserver);
  onBeforeUnmount(() => observer?.disconnect());
  watch(sentinel, setupObserver);
</script>
