// src/views/admin/users/UsersManagement.vue
<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <header>
        <PageHeader
          title="Users Management"
          description="Manage all users across all clinics"
          :stats="[{ icon: 'pi pi-users', label: `${visibleUsers.length} Users Visible` }]"
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
          <form autocomplete="off" @submit.prevent>
            <!-- Hidden input to trick autofill -->
            <input type="text" class="hidden" autocomplete="off" name="hidden-search-dummy" />
            <InputText
              v-model="filtersState.global"
              placeholder="Search users by name, email, role, or clinic..."
              class="w-full"
              autocomplete="new-password"
              name="user_search_query"
              id="user_search_query"
            />
          </form>
        </IconField>
        <small v-if="isFetching && visibleUsers.length > 0" class="mt-1 block text-surface-500">
          Updating results...
        </small>
        <small
          v-else-if="filtersState.global && !showGridLoading"
          class="mt-1 block text-surface-500"
        >
          Found results for "{{ filtersState.global }}"
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
          v-model:selected="selectedUserIds"
          :user="u"
        >
          <template #actions>
            <div class="flex gap-1">
              <template v-if="!u.isDeleted">
                <Button
                  v-if="canEdit"
                  v-tooltip.top="'Edit'"
                  icon="pi pi-pencil"
                  text
                  size="small"
                  severity="info"
                  @click="openEditDialog(u)"
                />
                <Button
                  v-if="canActivate"
                  v-tooltip.top="u.isActive ? 'Deactivate' : 'Activate'"
                  :icon="u.isActive ? 'pi pi-ban' : 'pi pi-check'"
                  text
                  size="small"
                  :severity="u.isActive ? 'warn' : 'success'"
                  :loading="loadingUserIds.has(u.id)"
                  @click="toggleUserStatus(u)"
                />
                <Button
                  v-if="canResetPassword"
                  v-tooltip.top="'Reset Password'"
                  icon="pi pi-key"
                  text
                  size="small"
                  severity="help"
                  @click="openResetPasswordDialog(u)"
                />
                <Button
                  v-tooltip.top="'Permissions'"
                  icon="pi pi-shield"
                  text
                  size="small"
                  severity="primary"
                  @click="openPermissionsDialog(u)"
                />
                <Button
                  v-if="canDelete"
                  v-tooltip.top="'Move to Trash'"
                  icon="pi pi-trash"
                  text
                  size="small"
                  severity="danger"
                  @click="openDeleteDialog(u)"
                />
              </template>

              <template v-else>
                <Button
                  v-if="canDelete"
                  v-tooltip.top="'Restore'"
                  icon="pi pi-undo"
                  text
                  size="small"
                  severity="success"
                  @click="restoreUser(u)"
                />
                <Button
                  v-if="canDelete"
                  v-tooltip.top="'Permanently Delete'"
                  icon="pi pi-times-circle"
                  text
                  size="small"
                  severity="danger"
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
        <ProgressSpinner style="width: 30px; height: 30px" stroke-width="4" />
      </div>

      <UserFilters
        v-model:visible="filtersDialogVisible"
        :filters="filtersState"
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

  // Composables & Types
  import { useUserActions } from '@/composables/query/users/useUserActions';
  import { useInfiniteUsersPaged } from '@/composables/query/users/useUsers';
  import { usePermission } from '@/composables/usePermission';
  import { useUserDialogs } from '@/composables/user/useUserDialog';
  import { useUserFilters } from '@/composables/user/useUserFilters';
  import type { PaginatedResult, UserResponseDto } from '@/types/backend';
  import type { InfiniteData } from '@tanstack/vue-query';
  import { refDebounced } from '@vueuse/core';
  import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

  const { can } = usePermission();

  // 1. Permissions
  const canCreate = computed(() => can('Users.Create'));
  const canEdit = computed(() => can('Users.Edit'));
  const canDelete = computed(() => can('Users.Delete'));
  const canResetPassword = computed(() => can('Users.ResetPassword'));
  const canActivate = computed(() => can('Users.Activate'));

  // 2. Filter State Management
  const { filtersState, resetFilters: resetLocalFilters } = useUserFilters();

  // 🌟 DEBOUNCING: Wait 300ms after user stops typing to trigger search
  const debouncedSearch = refDebounced(
    computed(() => filtersState.global),
    300,
  );

  // Computed Derived State for Query
  const includeDeleted = computed(
    () => filtersState.status === 'deleted' || filtersState.status === 'all',
  );

  // 3. Server-Side Data Fetching (Connected to Filters)
  const {
    data: infiniteData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isFetching,
    isLoading,
    refetch,
    error,
  } = useInfiniteUsersPaged({
    search: debouncedSearch,
    roleId: computed(() => filtersState.roleId),
    clinicId: computed(() => filtersState.clinicId),
    isActive: computed(() => filtersState.isActive),
    includeDeleted: includeDeleted,
    dateRange: computed(() => filtersState.dateRange),
  });

  // 4. Data Flattening
  const visibleUsers = computed(() => {
    const data = infiniteData.value as unknown as InfiniteData<PaginatedResult<UserResponseDto>>;
    return data?.pages.flatMap((page) => page.items ?? []) ?? [];
  });

  const errorMessage = computed(() => (error.value ? (error.value as any).message : null));

  // Show skeleton only on initial load OR if clearing filters creates a brief empty state
  const showGridLoading = computed(() => isLoading.value && visibleUsers.value.length === 0);

  // 5. Actions & Dialogs
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
  const loadingUserIds = ref(new Set<string>());

  const {
    createUserMutation,
    updateUserMutation,
    softDeleteMutation,
    hardDeleteMutation,
    restoreMutation,
    activateMutation,
    deactivateMutation,
  } = useUserActions();

  // 6. Event Handlers

  function applyFilters(newFilters: any) {
    Object.assign(filtersState, newFilters);
  }

  function resetAllFilters() {
    resetLocalFilters();
  }

  function toggleDeletedView() {
    if (filtersState.status === 'active') {
      filtersState.status = 'deleted';
    } else {
      filtersState.status = 'active';
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
    } catch (e: any) {
      // safe to ignore
    }
  }

  async function toggleUserStatus(user: UserResponseDto) {
    loadingUserIds.value.add(user.id);
    try {
      if (user.isActive) await deactivateMutation.mutateAsync(user.id);
      else await activateMutation.mutateAsync(user.id);
      // Tanstack Query handles invalidation automatically via useUserActions setup
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
        selectedUserIds.value = selectedUserIds.value.filter((id) => id !== selectedUser.value?.id);
        isDeleting.value = false;
      },
      onError: () => (isDeleting.value = false),
    });
  }

  function hardDeleteUser() {
    if (!selectedUser.value) return;
    isDeleting.value = true;
    hardDeleteMutation.mutate(selectedUser.value.id, {
      onSuccess: () => {
        closeAll();
        selectedUserIds.value = selectedUserIds.value.filter((id) => id !== selectedUser.value?.id);
        isDeleting.value = false;
      },
      onError: () => (isDeleting.value = false),
    });
  }

  function restoreUser(user: UserResponseDto) {
    restoreMutation.mutate(user.id);
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
    } finally {
      bulkActionLoading.value = false;
    }
  }

  // --- Infinite Scroll Observer ---
  const sentinel = ref<HTMLElement | null>(null);
  let observer: IntersectionObserver;

  function setupObserver() {
    if (observer) observer.disconnect();
    observer = new IntersectionObserver(async (entries) => {
      const entry = entries[0];
      if (entry && entry.isIntersecting && hasNextPage.value && !isFetchingNextPage.value) {
        await fetchNextPage();
      }
    });
    if (sentinel.value) observer.observe(sentinel.value);
  }

  onMounted(setupObserver);
  onBeforeUnmount(() => observer?.disconnect());
  watch(sentinel, setupObserver);
</script>
