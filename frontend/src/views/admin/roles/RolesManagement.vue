// src/views/admin/roles/RolesManagement.vue
<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <header>
        <PageHeader
          v-if="!initialLoading"
          title="Roles Management"
          description="Manage system roles, templates, and clinic roles"
          :stats="[{ icon: 'pi pi-unlock', label: `${filteredRoles.length} Total Roles` }]"
        />
        <div v-else class="space-y-3">
          <Skeleton height="2rem" width="40%" />
          <Skeleton height="1.2rem" width="60%" />
        </div>
      </header>

      <div v-if="initialLoading" class="flex items-center justify-between">
        <Skeleton height="2.5rem" width="140px" />
        <Skeleton height="2.5rem" width="140px" />
      </div>

      <RoleToolbar
        v-else
        :loading="isFetching"
        :include-deleted="includeDeleted"
        :can-create="canCreateRole"
        @create="openCreateDialog"
        @filters="filtersDialogVisible = true"
        @toggle-deleted="toggleDeletedView"
        @reset-filters="resetAllFilters"
        @refresh="refetch"
      />

      <div>
        <div v-if="initialLoading">
          <Skeleton height="2.7rem" width="100%" />
        </div>
        <div v-else>
          <IconField class="w-full">
            <InputIcon><i class="pi pi-search" /></InputIcon>
            <InputText
              v-model="activeFilters.global"
              placeholder="Search roles by name or description..."
              class="w-full"
            />
          </IconField>
          <small v-if="activeFilters.global" class="mt-1 block text-surface-500">
            {{ filteredRoles.length }} result{{ filteredRoles.length !== 1 ? 's' : '' }} found
          </small>
        </div>
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
        v-if="initialLoading"
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5"
      >
        <RoleCardSkeleton v-for="n in 8" :key="n" />
      </div>

      <RoleGrid v-else :roles="filteredRoles" :loading="false">
        <template #actions="{ role }">
          <div class="flex gap-1">
            <Button
              v-if="canEditRole && !role.isDeleted && !role.isSystemRole"
              icon="pi pi-pencil"
              text
              size="small"
              severity="info"
              v-tooltip.top="'Edit Role'"
              @click="openEditDialog(role)"
            />
            <Button
              v-if="canViewPermissions && !role.isDeleted"
              icon="pi pi-sliders-h"
              text
              size="small"
              severity="primary"
              v-tooltip.top="'Manage Permissions'"
              @click="openPermissionsDialog(role)"
            />
            <Button
              v-if="canCloneRole && role.isTemplate && !role.isDeleted"
              icon="pi pi-copy"
              text
              size="small"
              severity="help"
              v-tooltip.top="'Clone Template'"
              @click="openCloneDialog(role)"
            />
            <Button
              v-if="canDeleteRole && !role.isSystemRole && !role.isDeleted && role.userCount === 0"
              icon="pi pi-trash"
              text
              size="small"
              severity="danger"
              v-tooltip.top="'Delete Role'"
              @click="openDeleteDialog(role)"
            />
            <Button
              v-if="role.isDeleted && canRestoreRole"
              icon="pi pi-undo"
              text
              size="small"
              severity="success"
              v-tooltip.top="'Restore Role'"
              @click="restoreRole(role)"
            />
            <Button
              v-if="role.isDeleted && canDeleteRole && isSuperAdmin"
              icon="pi pi-times-circle"
              text
              size="small"
              severity="danger"
              v-tooltip.top="'Permanently Delete'"
              @click="openPermanentDeleteDialog(role)"
            />
          </div>
        </template>
      </RoleGrid>

      <RoleFilters
        v-model:visible="filtersDialogVisible"
        :filters="activeFilters"
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
  import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue';

  import RoleCardSkeleton from '@/components/Role/RoleCardSkeleton.vue';
  import RoleCreateEditDialog from '@/components/Role/RoleCreateEditDialog.vue';
  import RoleFilters from '@/components/Role/RoleFilters.vue';
  import RoleGrid from '@/components/Role/RoleGrid.vue';
  import RolePermissionsDialog from '@/components/Role/RolePermissionsDialog.vue';
  import RoleToolbar from '@/components/Role/RoleToolbar.vue';
  import PageHeader from '@/layout/PageHeader.vue';

  import { useRoleActions } from '@/composables/query/roles/useRoleActions';
  import { useRoles } from '@/composables/query/roles/useRoles';
  import { useRoleDialogs } from '@/composables/role/useRoleDialogs';
  import { usePermission } from '@/composables/usePermission';

  import Button from 'primevue/button';
  import ConfirmDialog from 'primevue/confirmdialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Skeleton from 'primevue/skeleton';

  import type { RoleResponseDto } from '@/types/backend';
  import { computed, ref } from 'vue';

  // 1. Data Query
  const {
    roles,
    filteredRoles,
    activeFilters,
    resetFilters: resetLocalFilters,
    isLoading,
    isFetching,
    error,
    refetch,
    includeDeleted,
  } = useRoles();

  const initialLoading = computed(() => isLoading.value && roles.value.length === 0);
  const errorMessage = computed(() =>
    error.value instanceof Error ? error.value.message : 'Failed to load roles',
  );

  // 2. Permissions & Actions
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

  // 3. Dialog State
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

  const filtersDialogVisible = ref(false);

  // ✅ Added local loading state for delete actions
  const isDeleting = ref(false);

  // ---------------------------------------
  // 4. FILTER LOGIC
  // ---------------------------------------
  function applyFilters(newFilters: any) {
    activeFilters.name = newFilters.name;
    activeFilters.isSystem = newFilters.isSystem;
    activeFilters.isTemplate = newFilters.isTemplate;
    activeFilters.dateRange = newFilters.dateRange;
    activeFilters.status = newFilters.status;

    const needsDeletedData = newFilters.status === 'deleted' || newFilters.status === 'all';

    if (needsDeletedData && !includeDeleted.value) {
      includeDeleted.value = true;
    } else if (!needsDeletedData && includeDeleted.value) {
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

  // 5. Action Handlers
  // ✅ Updated to handle loading state for the dialog
  async function handleDeleteRole() {
    if (!selectedRole.value) return;

    isDeleting.value = true;

    const onSuccess = () => {
      closeAll();
      refetch();
      isDeleting.value = false;
    };

    const onError = () => {
      isDeleting.value = false;
    };

    try {
      // Logic: If already deleted -> Hard Delete. If active -> Soft Delete.
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
    refetch();
  }
</script>
