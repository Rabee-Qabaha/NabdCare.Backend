<template>
  <div class="space-y-4 p-4 md:p-6">
    <div>
      <h2 class="text-2xl font-bold md:text-3xl">Roles Management</h2>
      <p class="text-600">Manage system roles, templates, and clinic roles</p>
    </div>

    <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
      <div class="flex flex-wrap gap-2">
        <Button 
          v-if="canCreateRole" 
          label="Create Role" 
          icon="pi pi-plus" 
          @click="openCreateDialog" 
        />
      </div>

      <div class="flex flex-wrap gap-2">
        <Button 
          icon="pi pi-sliders-h" 
          label="Filters" 
          outlined 
          @click="openFilters" 
        />
        <Button
          :label="includeDeleted ? 'Show Active' : 'Show Deleted'"
          :icon="includeDeleted ? 'pi pi-users' : 'pi pi-trash'"
          outlined
          @click="toggleIncludeDeleted"
        />
        <Button 
          icon="pi pi-filter-slash" 
          label="Clear Filters" 
          outlined 
          @click="resetFilters" 
        />
        <Button
          icon="pi pi-refresh"
          label="Refresh"
          outlined
          @click="refetch"
          :loading="isLoading"
        />
      </div>
    </div>

    <div>
      <IconField class="w-full">
        <InputIcon><i class="pi pi-search" /></InputIcon>
        <InputText
          v-model="searchQuery"
          placeholder="Search roles by name or description..."
          class="w-full"
        />
      </IconField>
      <small v-if="searchQuery" class="mt-1 block text-surface-500">
        {{ filteredRoles.length }} result{{ filteredRoles.length !== 1 ? 's' : '' }} found
      </small>
    </div>

    <div v-if="error" class="rounded-lg bg-red-50 p-4 dark:bg-red-900">
      <p class="text-red-800 dark:text-red-200">
        ❌ {{ error instanceof Error ? error.message : 'Failed to load roles' }}
      </p>
      <Button label="Retry" icon="pi pi-refresh" text @click="refetch" class="mt-2" />
    </div>

    <div
      v-if="isLoading"
      class="grid gap-4 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
    >
      <Card v-for="n in 8" :key="n">
        <template #header>
          <Skeleton height="100px" animation="wave" />
        </template>
        <template #title>
          <div class="flex items-start justify-between">
            <div class="flex flex-col gap-2 flex-1">
              <Skeleton width="8rem" height="1.5rem" animation="wave" />
              <Skeleton width="6rem" height="0.8rem" animation="wave" />
            </div>
          </div>
        </template>
        <template #content>
          <div class="mt-2 flex flex-col gap-2 text-sm">
            <div v-for="i in 3" :key="i">
              <Skeleton width="100%" height="0.9rem" animation="wave" />
            </div>
          </div>
        </template>
      </Card>
    </div>

    <div
      v-else-if="filteredRoles.length"
      class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4"
    >
      <div
        v-for="role in filteredRoles"
        :key="role.id"
        class="relative flex flex-col justify-between overflow-hidden rounded-2xl border shadow-sm transition-all duration-200"
        :class="[
          role.isDeleted
            ? 'border-pink-200 bg-pink-50 dark:border-pink-400 dark:bg-pink-550'
            : 'border-surface-200 bg-surface-0 dark:border-surface-700 dark:bg-surface-900',
          'hover:shadow-primary/40',
        ]"
      >
        <div>
          <div
            class="h-16 flex items-center justify-center text-4xl text-white" :style="{ backgroundColor: role.colorCode || '#3B82F6' }"
          >
            <i v-if="role.iconClass" :class="role.iconClass" />
            <i v-else class="pi pi-shield" />
          </div>

          <div class="p-4 pb-2">
            <div class="flex items-center justify-between mb-1">
              <div class="flex items-center gap-2">
                <h3 class="m-0 text-xl font-bold text-surface-900 dark:text-surface-0">
                  {{ role.name }}
                </h3>
                <Tag
                  :value="role.isSystemRole ? 'System Role' : 'Clinic Role'"
                  :severity="role.isSystemRole ? 'info' : 'success'"
                  rounded
                  class="text-xs"
                />
              </div>
              <Tag v-if="role.isDeleted" value="Deleted" severity="danger" rounded class="ml-2" />
            </div>
            <p class="m-0 text-sm text-surface-600 dark:text-surface-400 line-clamp-2">
              {{ role.description || 'No description' }}
            </p>
          </div>
          
          <div
            class="border-t px-4 py-3 text-sm"
            :class="
              role.isDeleted
                ? 'border-pink-200 dark:border-pink-700'
                : 'border-surface-200 dark:border-surface-700'
            "
          >
            <div class="space-y-1">
              <div class="flex items-center justify-between">
                <span class="flex items-center gap-2 text-surface-600 dark:text-surface-400">
                  <i class="pi pi-users text-surface-500"></i>
                  Users
                </span>
                <span class="truncate text-surface-700 dark:text-surface-200">
                  {{ role.userCount }}
                </span>
              </div>

              <div class="flex items-center justify-between">
                <span class="flex items-center gap-2 text-surface-600 dark:text-surface-400">
                  <i class="pi pi-sliders-h text-surface-500"></i>
                  Permissions
                </span>
                <span class="truncate text-surface-700 dark:text-surface-200">
                  {{ role.permissionCount }}
                </span>
              </div>

              <div class="flex items-center justify-between">
                <span class="flex items-center gap-2 text-surface-600 dark:text-surface-400">
                  <i class="pi pi-building text-surface-500"></i>
                  Clinic
                </span>
                <span class="truncate text-surface-700 dark:text-surface-200">
                  {{ role.clinicName || 'No Clinic' }}
                </span>
              </div>
            </div>

            <div
              class="mt-3 space-y-2 rounded-xl border px-3 py-2 text-xs"
              :class="
                role.isDeleted
                  ? 'border-pink-200 bg-pink-100/50 text-pink-800 dark:border-pink-700 dark:bg-pink-900/30 dark:text-pink-200'
                  : 'border-surface-200 bg-surface-50 text-surface-700 dark:border-surface-700 dark:bg-surface-800 dark:text-surface-300'
              "
            >
              <div class="flex items-center justify-between">
                <span class="flex items-center gap-1 font-medium">
                  <i class="pi pi-calendar text-green-500"></i>
                  Created
                </span>
                <span>{{ formatDate(role.createdAt) }}</span>
              </div>

              <div class="flex items-center justify-between">
                <span class="flex items-center gap-1 font-medium">
                  <i class="pi pi-refresh text-cyan-500"></i>
                  Updated
                </span>
                <span>{{ formatDate(role.updatedAt) }}</span>
              </div>
            </div>
          </div>
        </div>

        <div
          class="flex items-center justify-between border-t px-4 py-2"
          :class="
            role.isDeleted
              ? 'border-pink-200 bg-pink-100/30 dark:border-pink-700 dark:bg-pink-900/20'
              : 'border-surface-200 bg-surface-50 dark:border-surface-700 dark:bg-surface-800'
          "
        >
          <div class="font-mono text-xs text-surface-500 dark:text-surface-400">
            ID: {{ String(role.id).slice(0, 8) }}
          </div>

          <div class="flex gap-1">
            <template v-if="!role.isDeleted">
              <Button
                v-if="canEditRole && !role.isSystemRole"
                icon="pi pi-pencil"
                text
                size="small"
                severity="info"
                v-tooltip.top="'Edit role'"
                @click="openEditDialog(role)"
              />
              <Button
                v-if="canViewPermissions"
                icon="pi pi-sliders-h"
                text
                size="small"
                v-tooltip.top="'Manage permissions'"
                @click="openPermissionsDialog(role)"
              />
              <Button
                v-if="canCloneRole && role.isTemplate"
                icon="pi pi-copy"
                text
                size="small"
                v-tooltip.top="'Clone template'"
                @click="openCloneDialog(role)"
              />
              <Button
                v-if="canDeleteRole && !role.isSystemRole && role.userCount === 0"
                icon="pi pi-trash"
                text
                size="small"
                severity="danger"
                v-tooltip.top="'Delete role'"
                @click="openDeleteDialog(role)"
              />
              <Button
                v-if="canDeleteRole && !role.isSystemRole && role.userCount > 0"
                icon="pi pi-trash"
                text
                size="small"
                severity="danger"
                disabled
                v-tooltip.top="'Cannot delete: role has assigned users'"
              />
            </template>

            <template v-else>
              <Button
                v-if="canRestoreRole"
                icon="pi pi-undo"
                text
                size="small"
                severity="success"
                v-tooltip.top="'Restore role'"
                @click="restoreRole(role)"
              />
              <Button
                v-if="canDeleteRole && isSuperAdmin"
                icon="pi pi-trash"
                text
                size="small"
                severity="danger"
                v-tooltip.top="'Permanently delete'"
                @click="openPermanentDeleteDialog(role)"
              />
            </template>

            <Button
              icon="pi pi-info-circle"
              text
              size="small"
              v-tooltip.top="'View details'"
              @click="openDetailsDialog(role)"
            />
          </div>
        </div>
      </div>
    </div>
    <EmptyState
      v-else
      icon="pi pi-shield"
      title="No Roles Found"
      description="No roles match your search or filters"
    />

    <Dialog
      v-model:visible="filtersDialogVisible"
      header="Filters"
      modal
      :style="{ width: '500px', maxWidth: '95vw' }"
    >
      <div class="grid gap-4">
        <div>
          <label class="mb-2 block text-sm font-medium">Role Name</label>
          <InputText
            v-model="searchQuery"
            placeholder="Search by role name..."
            class="w-full"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium">Type</label>
          <Select
            v-model="typeFilter"
            :options="[
              { label: 'System Role', value: true },
              { label: 'Clinic Role', value: false },
            ]"
            optionLabel="label"
            optionValue="value"
            placeholder="Select type..."
            showClear
            class="w-full"
          />
        </div>

        <div>
          <label class="mb-2 block text-sm font-medium">Status</label>
          <Select
            v-model="statusFilterValue"
            :options="[
              { label: 'Active', value: false },
              { label: 'Deleted', value: true },
            ]"
            optionLabel="label"
            optionValue="value"
            placeholder="Select status..."
            showClear
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
            @click="
              () => {
                resetFilters();
                filtersDialogVisible = false;
              }
            "
          />
          <Button icon="pi pi-check" label="Apply Filters" @click="filtersDialogVisible = false" />
        </div>
      </template>
    </Dialog>

    <RoleCreateEditDialog
      v-model:visible="dialogs.createEdit"
      :role="selectedRole"
      @created="onRoleCreated"
      @updated="onRoleUpdated"
    />
    <RolePermissionsDialog
      v-model:visible="dialogs.permissions"
      :role="selectedRole"
      @updated="onPermissionsUpdated"
    />
    <RoleDetailsDialog
      v-model:visible="dialogs.details"
      :role="selectedRole"
    />
    <DeleteConfirmDialog
      v-model:visible="dialogs.deleteConfirm"
      :title="`Delete ${selectedRole?.name}?`"
      :message="`Are you sure you want to delete this role?${
        selectedRole?.userCount ?? 0 > 0
          ? ' It has ' + selectedRole?.userCount + ' assigned users.'
          : ''
      }`"
      @confirm="deleteRole"
    />
    <ConfirmDialog />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import type { RoleResponseDto } from '@/types/backend';
import { useRoles } from '@/composables/query/roles/useRoles';
import { useDeleteRole, useRestoreRole } from '@/composables/query/roles/useRoleActions';
import { usePermission } from '@/composables/usePermission';
import RoleCreateEditDialog from '@/components/Role/RoleCreateEditDialog.vue';
import RolePermissionsDialog from '@/components/Role/RolePermissionsDialog.vue';
import RoleDetailsDialog from '@/components/Role/RoleDetailsDialog.vue';
import DeleteConfirmDialog from '@/components/Role/DeleteConfirmDialog.vue';
import EmptyState from '@/components/EmptyState.vue';

// PrimeVue Components
import Card from 'primevue/card';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputIcon from 'primevue/inputicon';
import IconField from 'primevue/iconfield';
import Select from 'primevue/select';
import Tag from 'primevue/tag';
import Dialog from 'primevue/dialog';
import Skeleton from 'primevue/skeleton';
import ConfirmDialog from 'primevue/confirmdialog';

const { can: canPermission } = usePermission();
const { roles, filteredRoles, isLoading, error, refetch, includeDeleted } = useRoles();
const { mutate: deleteRoleMutation } = useDeleteRole();
const { mutate: restoreRoleMutation } = useRestoreRole();

// State
const searchQuery = ref('');
const typeFilter = ref<boolean | null>(null);
const statusFilterValue = ref<boolean | null>(null);
const filtersDialogVisible = ref(false);
const selectedRole = ref<RoleResponseDto | null>(null);

const dialogs = ref({
  createEdit: false,
  permissions: false,
  details: false,
  deleteConfirm: false,
  permanentDelete: false,
});

// Permissions
const canCreateRole = computed(() => canPermission('Roles.Create'));
const canEditRole = computed(() => canPermission('Roles.Edit'));
const canDeleteRole = computed(() => canPermission('Roles.Delete'));
const canRestoreRole = computed(() => canPermission('Roles.Restore'));
const canViewPermissions = computed(() => canPermission('Roles.Permissions.View'));
const canCloneRole = computed(() => canPermission('Roles.Clone'));
const isSuperAdmin = computed(() => canPermission('SuperAdmin'));

// Methods
const openFilters = () => {
  filtersDialogVisible.value = true;
};

const resetFilters = () => {
  searchQuery.value = '';
  typeFilter.value = null;
  statusFilterValue.value = null;
};

const toggleIncludeDeleted = () => {
  includeDeleted.value = !includeDeleted.value;
};

const openCreateDialog = () => {
  selectedRole.value = null;
  dialogs.value.createEdit = true;
};

const openEditDialog = (role: RoleResponseDto) => {
  selectedRole.value = role;
  dialogs.value.createEdit = true;
};

const openPermissionsDialog = (role: RoleResponseDto) => {
  selectedRole.value = role;
  dialogs.value.permissions = true;
};

const openDetailsDialog = (role: RoleResponseDto) => {
  selectedRole.value = role;
  dialogs.value.details = true;
};

const openDeleteDialog = (role: RoleResponseDto) => {
  selectedRole.value = role;
  dialogs.value.deleteConfirm = true;
};

const openCloneDialog = (role: RoleResponseDto) => {
  selectedRole.value = role;
  // TODO: Implement clone dialog
};

const openPermanentDeleteDialog = (role: RoleResponseDto) => {
  selectedRole.value = role;
  dialogs.value.permanentDelete = true;
};

const deleteRole = () => {
  if (!selectedRole.value) return;

  deleteRoleMutation(selectedRole.value.id, {
    onSuccess: () => {
      dialogs.value.deleteConfirm = false;
      refetch();
    },
  });
};

const restoreRole = (role: RoleResponseDto) => {
  restoreRoleMutation(role.id, {
    onSuccess: () => {
      refetch();
    },
  });
};

const onRoleCreated = () => {
  dialogs.value.createEdit = false;
  refetch();
};

const onRoleUpdated = () => {
  dialogs.value.createEdit = false;
  refetch();
};

const onPermissionsUpdated = () => {
  dialogs.value.permissions = false;
  refetch();
};

const formatDate = (date: string | Date): string => {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

onMounted(() => {
  refetch();
});
</script>