<template>
  <!-- Loading -->
  <div v-if="loading" class="grid gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
    <RoleCardSkeleton v-for="n in 8" :key="n" />
  </div>

  <!-- Empty -->
  <EmptyState
    v-else-if="!roles.length"
    icon="pi pi-shield"
    title="No Roles Found"
    description="No roles match your search or filters"
  />

  <!-- List -->
  <div v-else class="grid gap-5 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
    <RoleCard
      v-for="role in roles"
      :key="role.id"
      :role="role"
      :can-edit="canEdit"
      :can-delete="canDelete"
      :can-restore="canRestore"
      :can-clone="canClone"
      :can-permissions="canPermissions"
      @edit="$emit('edit', role)"
      @delete="$emit('delete', role)"
      @restore="$emit('restore', role)"
      @clone="$emit('clone', role)"
      @permissions="$emit('permissions', role)"
      @details="$emit('details', role)"
    />
  </div>
</template>

<script setup lang="ts">
  import EmptyState from '@/components/EmptyState.vue';
  import RoleCard from '@/components/Role/RoleCard.vue';
  import RoleCardSkeleton from '@/components/Role/RoleCardSkeleton.vue';

  defineProps<{
    roles: any[];
    loading: boolean;
    canEdit: boolean;
    canDelete: boolean;
    canRestore: boolean;
    canClone: boolean;
    canPermissions: boolean;
  }>();

  defineEmits(['edit', 'delete', 'restore', 'clone', 'permissions', 'details']);
</script>
