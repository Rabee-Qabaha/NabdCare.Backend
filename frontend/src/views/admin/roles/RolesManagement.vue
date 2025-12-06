<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <header>
        <PageHeader
          title="Roles Management"
          description="Manage system roles, templates, and clinic roles"
          :stats="[{ icon: 'pi pi-unlock', label: `${filteredRoles.length} Total Roles` }]"
        />
      </header>

      <RoleToolbar
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
        <IconField class="w-full">
          <InputIcon><i class="pi pi-search" /></InputIcon>
          <InputText
            v-model="activeFilters.global"
            placeholder="Search roles by name or description..."
            class="w-full"
          />
        </IconField>

        <small v-if="activeFilters.global && !showGridLoading" class="mt-1 block text-surface-500">
          {{ filteredRoles.length }} result{{ filteredRoles.length !== 1 ? 's' : '' }} found
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

      <RoleGrid :roles="filteredRoles" :loading="showGridLoading">
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
  import RoleCreateEditDialog from '@/components/Role/RoleCreateEditDialog.vue';
  import RoleFilters from '@/components/Role/RoleFilters.vue';
  import RoleGrid from '@/components/Role/RoleGrid.vue';
  import RolePermissionsDialog from '@/components/Role/RolePermissionsDialog.vue';
  import RoleToolbar from '@/components/Role/RoleToolbar.vue';
  import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue';
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

  import type { RoleResponseDto } from '@/types/backend';
  import { computed, ref } from 'vue';

  // 1. Data Query
  const {
    roles, // kept for checking lengths internally if needed, but RoleGrid uses filteredRoles
    filteredRoles,
    activeFilters,
    resetFilters: resetLocalFilters,
    isLoading,
    isFetching,
    error,
    refetch,
    includeDeleted,
  } = useRoles();

  const errorMessage = computed(() =>
    error.value instanceof Error ? error.value.message : 'Failed to load roles',
  );

  // Combine loading states.
  // If we are doing the initial load OR a background refetch (filtering/tab switch),
  // we want the grid to show skeletons.
  const showGridLoading = computed(() => isLoading.value || isFetching.value);

  // 2. Permissions
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
  const isDeleting = ref(false);

  // 4. Filters & Actions
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
