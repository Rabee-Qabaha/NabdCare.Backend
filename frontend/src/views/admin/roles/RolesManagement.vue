<template>
  <div class="card">
    <div class="space-y-4 p-4 md:p-6">
      <!-- ===================== PAGE HEADER ===================== -->
      <header>
        <!-- Show real header if we have data OR background refetching -->
        <PageHeader
          v-if="!initialLoading"
          title="Roles Management"
          description="Manage system roles, templates, and clinic roles"
          :stats="[{ icon: 'pi pi-unlock', label: `${filteredRoles.length} Total Roles` }]"
        />

        <!-- Skeleton only on first load (no cached data) -->
        <div v-else class="space-y-3">
          <Skeleton height="2rem" width="40%" />
          <Skeleton height="1.2rem" width="60%" />
        </div>
      </header>

      <!-- ===================== TOOLBAR ===================== -->
      <template v-if="initialLoading">
        <div class="flex items-center justify-between">
          <Skeleton height="2.5rem" width="140px" />
          <Skeleton height="2.5rem" width="140px" />
        </div>
      </template>

      <RoleToolbar
        v-else
        :loading="isFetching"
        :include-deleted="includeDeleted"
        :can-create="canCreateRole"
        @create="openCreateDialog"
        @filters="openFilters"
        @toggle-deleted="toggleIncludeDeleted"
        @reset-filters="resetFilters"
        @refresh="refetch"
      />

      <!-- ===================== SEARCH BAR ===================== -->
      <div>
        <template v-if="initialLoading">
          <Skeleton height="2.7rem" width="100%" />
        </template>

        <template v-else>
          <IconField class="w-full">
            <InputIcon><i class="pi pi-search" /></InputIcon>
            <InputText
              v-model="searchQuery"
              placeholder="Search roles by name or description..."
              class="w-full"
            />
          </IconField>

          <small v-if="searchQuery" class="mt-1 block text-surface-500">
            {{ filteredRoles.length }}
            result{{ filteredRoles.length !== 1 ? 's' : '' }} found
          </small>
        </template>
      </div>

      <!-- ===================== ERROR ===================== -->
      <div v-if="error" class="rounded-lg bg-red-50 p-4 dark:bg-red-900">
        <p class="text-red-800 dark:text-red-200">❌ {{ errorMessage }}</p>
        <Button label="Retry" icon="pi pi-refresh" text @click="refetch" class="mt-2" />
      </div>

      <!-- ===================== ROLE GRID ===================== -->
      <!-- Skeleton first-time only -->
      <div
        v-if="initialLoading"
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5"
      >
        <RoleCardSkeleton v-for="n in 8" :key="n" />
      </div>

      <!-- Real content -->
      <RoleGrid v-else :roles="filteredRoles" :loading="false">
        <template #actions="{ role }">
          <div class="flex gap-1">
            <!-- EDIT -->
            <Button
              v-if="canEditRole && !role.isDeleted && !role.isSystemRole"
              icon="pi pi-pencil"
              text
              size="small"
              severity="info"
              v-tooltip.top="'Edit role'"
              @click="openEditDialog(role)"
            />

            <!-- PERMISSIONS -->
            <Button
              v-if="canViewPermissions && !role.isDeleted"
              icon="pi pi-sliders-h"
              text
              size="small"
              v-tooltip.top="'Manage permissions'"
              @click="openPermissionsDialog(role)"
            />

            <!-- CLONE -->
            <Button
              v-if="canCloneRole && role.isTemplate && !role.isDeleted"
              icon="pi pi-copy"
              text
              size="small"
              v-tooltip.top="'Clone template'"
              @click="openCloneDialog(role)"
            />

            <!-- DELETE -->
            <Button
              v-if="canDeleteRole && !role.isSystemRole && !role.isDeleted && role.userCount === 0"
              icon="pi pi-trash"
              text
              size="small"
              severity="danger"
              v-tooltip.top="'Delete role'"
              @click="openDeleteDialog(role)"
            />

            <!-- RESTORE -->
            <Button
              v-if="role.isDeleted && canRestoreRole"
              icon="pi pi-undo"
              text
              size="small"
              severity="success"
              v-tooltip.top="'Restore role'"
              @click="restoreRole(role)"
            />

            <!-- PERMANENT DELETE -->
            <Button
              v-if="role.isDeleted && canDeleteRole && isSuperAdmin"
              icon="pi pi-times-circle"
              text
              size="small"
              severity="danger"
              v-tooltip.top="'Permanently delete'"
              @click="openPermanentDeleteDialog(role)"
            />

            <!-- DETAILS -->
            <Button
              icon="pi pi-info-circle"
              text
              size="small"
              v-tooltip.top="'View details'"
              @click="openDetailsDialog(role)"
            />
          </div>
        </template>
      </RoleGrid>

      <!-- FILTERS DIALOG -->
      <RoleFilters
        v-model:visible="filtersDialogVisible"
        :search="searchQuery"
        :type="typeFilter"
        :deleted="statusFilterValue"
        @apply="applyFilters"
        @reset="resetFilters"
      />

      <!-- DIALOGS -->
      <RoleCreateEditDialog
        v-model:visible="dialogs.createEdit"
        :role="selectedRole"
        @saved="onRoleSaved"
      />

      <RolePermissionsDialog
        v-model:visible="dialogs.permissions"
        :role-id="selectedRole?.id ?? null"
        @updated="onPermissionsUpdated"
      />

      <RoleDetailsDialog
        v-model:visible="dialogs.details"
        :role-id="selectedRole?.id ?? null"
        :role="selectedRole"
      />

      <!-- DELETE CONFIRM -->
      <DeleteConfirmDialog
        v-model:visible="dialogs.deleteConfirm"
        :title="`Delete ${selectedRole?.name}?`"
        :message="`Are you sure you want to delete this role?`"
        @confirm="deleteRole"
      />

      <ConfirmDialog />
    </div>
  </div>
</template>

<script setup lang="ts">
  /* COMPONENTS */
  import DeleteConfirmDialog from '@/components/Role/DeleteConfirmDialog.vue';
  import RoleCardSkeleton from '@/components/Role/RoleCardSkeleton.vue';
  import RoleCreateEditDialog from '@/components/Role/RoleCreateEditDialog.vue';
  import RoleDetailsDialog from '@/components/Role/RoleDetailsDialog.vue';
  import RoleFilters from '@/components/Role/RoleFilters.vue';
  import RoleGrid from '@/components/Role/RoleGrid.vue';
  import RolePermissionsDialog from '@/components/Role/RolePermissionsDialog.vue';
  import RoleToolbar from '@/components/Role/RoleToolbar.vue';
  import PageHeader from '@/layout/PageHeader.vue';

  /* COMPOSABLE */
  import { useRoleActions } from '@/composables/query/roles/useRoleActions';
  import { useRoles } from '@/composables/query/roles/useRoles';
  import { usePermission } from '@/composables/usePermission';

  /* PRIMEVUE */
  import Button from 'primevue/button';
  import ConfirmDialog from 'primevue/confirmdialog';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Skeleton from 'primevue/skeleton';

  /* VUE */
  import type { RoleResponseDto } from '@/types/backend';
  import { computed, ref } from 'vue';

  /* ROLES QUERY (SWR MODE) */
  const { roles, filteredRoles, isLoading, isFetching, error, refetch, includeDeleted } =
    useRoles();

  /* Loading states */
  const initialLoading = computed(() => isLoading.value && roles.value.length === 0);

  /* Error normalization */
  const errorMessage = computed(() =>
    error.value instanceof Error ? error.value.message : 'Failed to load roles',
  );

  /* Permissions */
  const { can: canPermission } = usePermission();
  const canViewPermissions = computed(() => canPermission('Roles.Permissions.View'));
  const isSuperAdmin = computed(() => canPermission('SuperAdmin'));

  /* Filters */
  const searchQuery = ref('');
  const typeFilter = ref<boolean | null>(null);
  const statusFilterValue = ref<boolean | null>(null);
  const filtersDialogVisible = ref(false);

  /* Dialogs */
  const dialogs = ref({
    createEdit: false,
    permissions: false,
    details: false,
    deleteConfirm: false,
    permanentDelete: false,
  });
  const selectedRole = ref<RoleResponseDto | null>(null);

  /* Filters */
  function applyFilters(filters: any) {
    searchQuery.value = filters.search;
    typeFilter.value = filters.type;
    statusFilterValue.value = filters.deleted;
  }
  function resetFilters() {
    searchQuery.value = '';
    typeFilter.value = null;
    statusFilterValue.value = null;
  }

  /* Dialog Controls */
  function openCreateDialog() {
    selectedRole.value = null;
    dialogs.value.createEdit = true;
  }
  function openEditDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.createEdit = true;
  }
  function openCloneDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.createEdit = true;
  }
  function openPermissionsDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.permissions = true;
  }
  function openDetailsDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.details = true;
  }
  function openDeleteDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.deleteConfirm = true;
  }
  function openPermanentDeleteDialog(role: RoleResponseDto) {
    selectedRole.value = role;
    dialogs.value.permanentDelete = true;
  }

  /* Delete / Restore */
  const {
    deleteRole: deleteRoleAction,
    restoreRole: restoreRoleAction,
    canEditRole,
    canDeleteRole,
    canRestoreRole,
    canCloneRole,
    canCreateRole,
  } = useRoleActions();

  function deleteRole() {
    if (!selectedRole.value) return;
    deleteRoleAction(selectedRole.value.id, {
      onSuccess: () => {
        dialogs.value.deleteConfirm = false;
        refetch();
      },
    });
  }
  function restoreRole(role: RoleResponseDto) {
    restoreRoleAction(role.id, { onSuccess: () => refetch() });
  }

  /* Callbacks */
  function onRoleSaved() {
    dialogs.value.createEdit = false;
    refetch();
  }
  function onPermissionsUpdated() {
    dialogs.value.permissions = false;
    refetch();
  }
</script>
