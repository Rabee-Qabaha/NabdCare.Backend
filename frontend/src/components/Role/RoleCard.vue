<template>
  <div
    class="relative flex flex-col justify-between overflow-hidden rounded-2xl border shadow-sm transition-all duration-200"
    :class="[
      role.isDeleted
        ? 'border-pink-200 bg-pink-50 dark:border-pink-400 dark:bg-pink-550'
        : 'border-surface-200 bg-surface-0 dark:border-surface-700 dark:bg-surface-900',
      'hover:shadow-primary/40',
    ]"
  >
    <!-- Icon area -->
    <div
      class="h-16 flex items-center justify-center text-4xl text-white"
      :style="{ backgroundColor: role.colorCode || '#3B82F6' }"
    >
      <i v-if="role.iconClass" :class="role.iconClass"></i>
      <i v-else class="pi pi-shield"></i>
    </div>

    <!-- Main content -->
    <div class="p-4 pb-2">
      <div class="flex items-center justify-between mb-1">
        <h3 class="m-0 text-xl font-bold">{{ role.name }}</h3>
        <Tag
          :value="role.isSystemRole ? 'System Role' : 'Clinic Role'"
          :severity="role.isSystemRole ? 'info' : 'success'"
          rounded
          class="text-xs"
        />
      </div>
      <p class="m-0 text-sm text-surface-600 dark:text-surface-400 line-clamp-2">
        {{ role.description || 'No description' }}
      </p>
    </div>

    <!-- Stats -->
    <div
      class="border-t px-4 py-3 text-sm flex flex-col gap-1"
      :class="role.isDeleted ? 'border-pink-200' : 'border-surface-200'"
    >
      <div class="flex items-center justify-between">
        <span>
          <i class="pi pi-users"></i>
          Users
        </span>
        <span>{{ role.userCount }}</span>
      </div>

      <div class="flex items-center justify-between">
        <span>
          <i class="pi pi-sliders-h"></i>
          Permissions
        </span>
        <span>{{ role.permissionCount }}</span>
      </div>

      <div class="flex items-center justify-between">
        <span>
          <i class="pi pi-building"></i>
          Clinic
        </span>
        <span>{{ role.clinicName || 'No Clinic' }}</span>
      </div>
    </div>

    <!-- Footer -->
    <div class="flex items-center justify-between border-t px-4 py-2">
      <div class="font-mono text-xs">ID: {{ role.id.slice(0, 8) }}</div>
      <slot name="actions" :role="role"></slot>
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { RoleResponseDto } from '@/types/backend';

  defineProps<{
    role: RoleResponseDto;
  }>();
</script>
