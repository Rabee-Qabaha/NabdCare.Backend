// src/components/User/UserToolbar.vue
<template>
  <div
    class="flex flex-col gap-3 rounded-xl border border-surface-200 bg-white p-3 shadow-sm dark:border-surface-700 dark:bg-surface-900 md:flex-row md:items-center md:justify-between"
  >
    <div class="flex flex-wrap gap-2">
      <Button
        v-if="canCreate"
        label="Create User"
        icon="pi pi-plus"
        class="w-full font-semibold shadow-sm sm:w-auto"
        @click="$emit('create')"
      />

      <div v-if="hasSelection" class="flex gap-2">
        <Button
          v-if="canActivate"
          label="Activate"
          icon="pi pi-check"
          severity="success"
          outlined
          size="small"
          @click="$emit('bulk-activate')"
        />
        <Button
          v-if="canActivate"
          label="Deactivate"
          icon="pi pi-ban"
          severity="warning"
          outlined
          size="small"
          @click="$emit('bulk-deactivate')"
        />
        <Button
          v-if="canDelete"
          label="Delete"
          icon="pi pi-trash"
          severity="danger"
          outlined
          size="small"
          @click="$emit('bulk-delete')"
        />
      </div>
    </div>

    <div class="flex flex-wrap items-center gap-2">
      <Button
        label="Filters"
        icon="pi pi-sliders-h"
        severity="secondary"
        outlined
        class="w-full sm:w-auto"
        @click="$emit('filters')"
      />

      <div class="mx-1 hidden h-6 w-px bg-surface-200 dark:bg-surface-700 md:block"></div>

      <Button
        :label="includeDeleted ? 'Back to Active' : 'Recycle Bin'"
        :icon="includeDeleted ? 'pi pi-arrow-left' : 'pi pi-trash'"
        :severity="includeDeleted ? 'danger' : 'secondary'"
        :variant="includeDeleted ? 'outlined' : 'text'"
        class="w-full sm:w-auto"
        :class="{ 'bg-red-50 dark:bg-red-900/20': includeDeleted }"
        @click="$emit('toggle-deleted')"
      />

      <div class="flex w-full justify-end gap-1 sm:w-auto">
        <Button
          icon="pi pi-filter-slash"
          severity="secondary"
          text
          rounded
          v-tooltip.top="'Reset filters'"
          @click="$emit('reset-filters')"
        />
        <Button
          icon="pi pi-refresh"
          severity="secondary"
          text
          rounded
          :loading="loading"
          v-tooltip.top="'Refresh list'"
          @click="$emit('refresh')"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import Button from 'primevue/button';

  defineProps<{
    loading: boolean;
    includeDeleted: boolean;
    canCreate: boolean;
    canActivate: boolean;
    canDelete: boolean;
    hasSelection: boolean;
  }>();

  defineEmits([
    'create',
    'filters',
    'toggle-deleted',
    'reset-filters',
    'refresh',
    'bulk-activate',
    'bulk-deactivate',
    'bulk-delete',
  ]);
</script>
