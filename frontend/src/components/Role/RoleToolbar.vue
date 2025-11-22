<template>
  <div class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
    <!-- Left: Create button -->
    <div class="flex flex-wrap gap-2">
      <Button v-if="canCreate" label="Create Role" icon="pi pi-plus" @click="$emit('create')" />
    </div>

    <!-- Right: actions -->
    <div class="flex flex-wrap gap-2">
      <Button icon="pi pi-sliders-h" label="Filters" outlined @click="$emit('filters')" />
      <Button
        :label="includeDeleted ? 'Show Active' : 'Show Deleted'"
        :icon="includeDeleted ? 'pi pi-users' : 'pi pi-trash'"
        outlined
        @click="$emit('toggle-deleted')"
      />
      <Button
        icon="pi pi-filter-slash"
        label="Clear Filters"
        outlined
        @click="$emit('reset-filters')"
      />
      <Button
        icon="pi pi-refresh"
        label="Refresh"
        outlined
        @click="$emit('refresh')"
        :loading="loading"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
  defineProps<{
    loading: boolean;
    includeDeleted: boolean;
    canCreate: boolean;
  }>();

  defineEmits(['create', 'filters', 'toggle-deleted', 'reset-filters', 'refresh']);
</script>
