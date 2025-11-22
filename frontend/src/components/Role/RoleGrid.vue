<!-- src/components/Role/RoleGrid.vue -->
<template>
  <!-- Loading Skeletons -->
  <div v-if="loading" class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4">
    <RoleCardSkeleton v-for="n in 8" :key="n" />
  </div>

  <!-- Empty State -->
  <EmptyState
    v-else-if="roles.length === 0"
    icon="pi pi-shield"
    title="No Roles Found"
    description="No roles match your filters"
  />

  <!-- Real Roles -->
  <div v-else class="grid gap-5 sm:grid-cols-2 md:gap-6 lg:grid-cols-3 xl:grid-cols-4">
    <RoleCard v-for="role in roles" :key="role.id" :role="role">
      <template #actions="{ role }">
        <slot name="actions" :role="role" />
      </template>
    </RoleCard>
  </div>
</template>

<script setup lang="ts">
  import EmptyState from '@/components/EmptyState.vue';
  import RoleCard from '@/components/Role/RoleCard.vue';
  import RoleCardSkeleton from '@/components/Role/RoleCardSkeleton.vue';

  defineProps<{
    roles: any[];
    loading: boolean;
  }>();
</script>
