<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <header>
        <PageHeader
          title="Roles Management"
          description="Manage system roles, templates, and clinic roles"
          :stats="[{ icon: 'pi pi-unlock', label: `${visibleRoles.length} Visible` }]"
        />
      </header>

      <RoleToolbar
        :loading="isFetching"
        :include-deleted="filtersState.status !== 'active'"
        :can-create="canCreateRole"
        @create="openCreateDialog"
        @filters="filtersDialogVisible = true"
        @toggle-deleted="toggleDeletedView"
        @reset-filters="resetAllFilters"
        @refresh="refetch"
      />

      <div>
        <IconField class="w-full">
          <InputIcon><i class="pi pi-search" /></InputIcon>
          <InputText
            v-model="filtersState.global"
            placeholder="Search roles by name..."
            class="w-full"
          />
        </IconField>

        <small v-if="filtersState.global && !showGridLoading" class="mt-1 block text-surface-500">
          Found results for "{{ filtersState.global }}"
        </small>
      </div>

      <div
        v-if="error"
        class="rounded-lg bg-red-50 p-4 dark:bg-red-900/20 border border-red-200 dark:border-red-800"
      >
        <div class="flex items-center gap-2 text-red-800 dark:text-red-200 mb-2">
          <i class="pi pi-exclamation-circle"></i>
          <span class="font-bold">Error loading roles</span>
        </div>
        <p class="text-sm text-red-700 dark:text-red-300 mb-2">{{ errorMessage }}</p>
        <Button
          label="Retry"
          icon="pi pi-refresh"
          text
          severity="danger"
          size="small"
          @click="refetch()"
        />
      </div>

      <div
        v-if="showGridLoading"
        class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
      >
        <div
          v-for="n in 8"
          :key="n"
          class="h-48 rounded-xl bg-surface-100 dark:bg-surface-800 animate-pulse border border-surface-200 dark:border-surface-700"
        ></div>
      </div>

      <RoleGrid v-else :roles="visibleRoles" :loading="false">
        <template #actions="{ role }">
          <div class="flex gap-1">
            <Button
              v-if="canEditRole && !role.isDeleted && !role.isSystemRole"
              v-tooltip.top="'Edit Role'"
              icon="pi pi-pencil"
              text
              size="small"
              severity="info"
              @click="openEditDialog(role)"
            />
            <Button
              v-if="canViewPermissions && !role.isDeleted"
              v-tooltip.top="'Manage Permissions'"
              icon="pi pi-sliders-h"
              text
              size="small"
              severity="primary"
              @click="openPermissionsDialog(role)"
            />
            <Button
              v-if="canCloneRole && role.isTemplate && !role.isDeleted"
              v-tooltip.top="'Clone Template'"
              icon="pi pi-copy"
              text
              size="small"
              severity="help"
              @click="openCloneDialog(role)"
            />
            <Button
              v-if="canDeleteRole && !role.isSystemRole && !role.isDeleted && role.userCount === 0"
              v-tooltip.top="'Delete Role'"
              icon="pi pi-trash"
              text
              size="small"
              severity="danger"
              @click="openDeleteDialog(role)"
            />
            <Button
              v-if="role.isDeleted && canRestoreRole"
              v-tooltip.top="'Restore Role'"
              icon="pi pi-undo"
              text
              size="small"
              severity="success"
              @click="restoreRole(role)"
            />
            <Button
              v-if="role.isDeleted && canDeleteRole && isSuperAdmin"
              v-tooltip.top="'Permanently Delete'"
              icon="pi pi-times-circle"
              text
              size="small"
              severity="danger"
              @click="openPermanentDeleteDialog(role)"
            />
          </div>
        </template>
      </RoleGrid>

      <div ref="sentinel" class="h-6 w-full"></div>
      <div v-if="isFetchingNextPage" class="flex justify-center p-4">
        <ProgressSpinner style="width: 30px; height: 30px" stroke-width="4" />
      </div>

      <RoleFilters
        v-model:visible="filtersDialogVisible"
        :filters="filtersState"
        @apply="applyFilters"
        @reset="resetAllFilters"
      />

      <RoleCreateEditDialog
        v-model:visible="dialogs.createEdit"
        :role="selectedRole"
        :mode="dialogMode"
        @saved="onRoleSaved"
      />

      <RolePermissionsDialog
        v-model:visible="dialogs.permissions"
        :role-id="selectedRole?.id ?? null"
        :role="selectedRole"
        @updated="onPermissionsUpdated"
      />

      <DeleteConfirmDialog
        v-model:visible="dialogs.deleteConfirm"
        mode="soft"
        title="Move to Trash?"
        :message="`Are you sure you want to disable this role? Users assigned to this role will lose their permissions.`"
        :item-identifier="selectedRole?.name"
        :loading="isDeleting"
        @confirm="handleDeleteRole"
      />

      <DeleteConfirmDialog
        v-model:visible="dialogs.permanentDelete"
        mode="hard"
        :item-identifier="selectedRole?.name"
        :loading="isDeleting"
        @confirm="handleDeleteRole"
      />

      <ConfirmDialog />
    </div>
  </div>
</template>

<script setup lang="ts">
  // Vue & Utils
  import { refDebounced } from '@vueuse/core';
  import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

  // PrimeVue Components
  import Button from 'primevue/button';
  import ConfirmDialog from 'primevue/confirmdialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import ProgressSpinner from 'primevue/progressspinner';

  // Custom Components
  import RoleCreateEditDialog from '@/components/Role/RoleCreateEditDialog.vue';
  import RoleFilters from '@/components/Role/RoleFilters.vue';
  import RoleGrid from '@/components/Role/RoleGrid.vue';
  import RolePermissionsDialog from '@/components/Role/RolePermissionsDialog.vue';
  import RoleToolbar from '@/components/Role/RoleToolbar.vue';
  import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue';
  import PageHeader from '@/layout/PageHeader.vue';

  // Composables
  import { useRoleActions } from '@/composables/query/roles/useRoleActions';
  import { useInfiniteRolesPaged } from '@/composables/query/roles/useRoles';
  import { useRoleDialogs } from '@/composables/role/useRoleDialogs';
  import { useRoleFilters } from '@/composables/role/useRoleFilters';
  import { usePermission } from '@/composables/usePermission';

  // Types
  import type { PaginatedResult, RoleResponseDto } from '@/types/backend';
  import type { InfiniteData } from '@tanstack/vue-query';

  // 1. Filter State Management
  const { filtersState, resetFilters } = useRoleFilters();
  const filtersDialogVisible = ref(false);

  // Debounce search input (300ms)
  const debouncedSearch = refDebounced(
    computed(() => filtersState.global),
    300,
  );

  // 2. Server-Side Data Query
  const {
    data: infiniteData,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isFetching,
    isLoading,
    refetch,
    error,
  } = useInfiniteRolesPaged({
    search: debouncedSearch,
    clinicId: computed(() => null), // Admin view usually sees all visible roles; filters can narrow this if needed
    roleOrigin: computed(() => filtersState.roleOrigin),
    isTemplate: computed(() => filtersState.isTemplate),
    status: computed(() => filtersState.status),
    dateRange: computed(() => filtersState.dateRange),
  });

  // 3. Flatten Pagination Pages for Display
  const visibleRoles = computed(() => {
    const data = infiniteData.value as unknown as InfiniteData<PaginatedResult<RoleResponseDto>>;
    return data?.pages.flatMap((page) => page.items ?? []) ?? [];
  });

  const errorMessage = computed(() =>
    error.value instanceof Error ? error.value.message : 'Failed to load roles',
  );

  // Smart loading state: Show skeletons only on initial load or empty search results
  const showGridLoading = computed(() => {
    if (isLoading.value) return true;
    if (isFetching.value && visibleRoles.value.length === 0) return true;
    return false;
  });

  // 4. Permissions
  const { can: canPermission } = usePermission();
  const canViewPermissions = computed(() => canPermission('Roles.Permissions.View'));
  const isSuperAdmin = computed(() => canPermission('SuperAdmin'));

  const {
    deleteRole: deleteRoleAction,
    hardDeleteRole: hardDeleteRoleAction,
    restoreRole: restoreRoleAction,
    canEditRole,
    canDeleteRole,
    canRestoreRole,
    canCloneRole,
    canCreateRole,
  } = useRoleActions();

  // 5. Dialogs & Actions
  const {
    dialogs,
    selectedRole,
    dialogMode,
    openCreateDialog,
    openEditDialog,
    openCloneDialog,
    openPermissionsDialog,
    openDeleteDialog,
    openPermanentDeleteDialog,
    closeAll,
  } = useRoleDialogs();

  const isDeleting = ref(false);

  // --- Handlers ---

  function applyFilters(newFilters: any) {
    // Update reactive state, triggering the query automatically
    Object.assign(filtersState, newFilters);
  }

  function resetAllFilters() {
    resetFilters();
  }

  function toggleDeletedView() {
    filtersState.status = filtersState.status === 'active' ? 'deleted' : 'active';
  }

  async function handleDeleteRole() {
    if (!selectedRole.value) return;
    isDeleting.value = true;

    const onSuccess = () => {
      closeAll();
      // No need to manually filter local array; refetch or cache invalidation handles it
      // Standard practice: Refetch to ensure count/pagination is correct
      refetch();
      isDeleting.value = false;
    };
    const onError = () => {
      isDeleting.value = false;
    };

    try {
      if (selectedRole.value.isDeleted) {
        await hardDeleteRoleAction(selectedRole.value.id, { onSuccess, onError });
      } else {
        await deleteRoleAction(selectedRole.value.id, { onSuccess, onError });
      }
    } catch (e) {
      isDeleting.value = false;
    }
  }

  function restoreRole(role: RoleResponseDto) {
    restoreRoleAction(role.id, { onSuccess: () => refetch() });
  }

  function onRoleSaved() {
    closeAll();
    refetch();
  }

  function onPermissionsUpdated() {
    dialogs.value.permissions = false;
    // Permissions updated -> Refetch roles to update permission counts if necessary
    refetch();
  }

  // 6. Infinite Scroll Observer
  const sentinel = ref<HTMLElement | null>(null);
  let observer: IntersectionObserver;

  function setupObserver() {
    if (observer) observer.disconnect();

    observer = new IntersectionObserver(async (entries) => {
      const entry = entries[0];
      // Trigger load if visible, has more pages, and not currently loading
      if (entry && entry.isIntersecting && hasNextPage.value && !isFetchingNextPage.value) {
        await fetchNextPage();
      }
    });

    if (sentinel.value) observer.observe(sentinel.value);
  }

  // Setup observer on mount and watch for ref changes
  onMounted(setupObserver);
  onBeforeUnmount(() => observer?.disconnect());
  watch(sentinel, setupObserver);
</script>
