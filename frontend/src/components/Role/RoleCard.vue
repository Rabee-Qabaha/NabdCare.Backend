<template>
  <BaseCard no-padding>
    <div v-if="role.isDeleted" class="absolute top-0 left-0 w-full h-1 bg-red-500 z-10"></div>

    <div class="p-5 pb-2">
      <div class="flex justify-between items-start mb-4">
        <div
          class="flex items-center justify-center w-12 h-12 rounded-xl text-white shadow-sm shrink-0"
          :style="{ backgroundColor: role.colorCode || '#64748b' }"
        >
          <i :class="[role.iconClass || 'pi pi-shield', 'text-xl']"></i>
        </div>

        <div class="flex flex-col items-end gap-2">
          <Tag
            :value="role.isSystemRole ? 'System' : 'Clinic'"
            :severity="role.isSystemRole ? 'warn' : 'success'"
            class="!text-[10px] uppercase font-bold tracking-wide !px-2"
          />
          <span
            v-if="role.isDeleted"
            class="text-[10px] font-bold text-red-600 dark:text-red-400 uppercase bg-red-100 dark:bg-red-900/30 px-2 py-0.5 rounded"
          >
            Deleted
          </span>
        </div>
      </div>

      <div class="mb-2">
        <h3
          class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight mb-1 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors"
        >
          {{ role.name }}
        </h3>
        <p class="text-sm text-surface-500 dark:text-surface-400 leading-relaxed line-clamp-2 h-10">
          {{ role.description || 'No description provided.' }}
        </p>
      </div>
    </div>

    <div class="mt-auto">
      <div
        class="grid grid-cols-2 border-t border-surface-200 dark:border-surface-700 divide-x divide-surface-200 dark:divide-surface-700 bg-surface-50/50 dark:bg-surface-800/30"
      >
        <div
          class="p-3 flex flex-col items-center justify-center hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors"
        >
          <span
            class="text-[10px] font-bold text-surface-500 dark:text-surface-400 uppercase tracking-wide"
          >
            Users
          </span>
          <div
            class="flex items-center gap-1.5 text-surface-900 dark:text-surface-0 font-bold text-sm"
          >
            <i class="pi pi-users text-primary-500 dark:text-primary-400 text-xs"></i>
            <span>{{ role.userCount }}</span>
          </div>
        </div>

        <div
          class="p-3 flex flex-col items-center justify-center hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors"
        >
          <span
            class="text-[10px] font-bold text-surface-500 dark:text-surface-400 uppercase tracking-wide"
          >
            Perms
          </span>
          <div
            class="flex items-center gap-1.5 text-surface-900 dark:text-surface-0 font-bold text-sm"
          >
            <i class="pi pi-key text-primary-500 dark:text-primary-400 text-xs"></i>
            <span>{{ role.permissionCount }}</span>
          </div>
        </div>
      </div>

      <div
        class="flex items-center justify-between p-3 border-t border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 rounded-b-xl"
      >
        <div class="flex flex-col">
          <div
            class="flex items-center gap-1 text-xs text-surface-500 dark:text-surface-400 font-medium max-w-[120px] truncate"
          >
            <i class="pi pi-building text-[10px] opacity-70"></i>
            <span :title="role.clinicName">{{ role.clinicName || 'Global' }}</span>
          </div>
          <span class="text-[9px] text-surface-400 dark:text-surface-500 font-mono mt-0.5">
            #{{ role.id.slice(0, 6) }}
          </span>
        </div>

        <div class="flex gap-2">
          <slot name="actions" :role="role"></slot>
        </div>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import type { RoleResponseDto } from '@/types/backend';
  import Tag from 'primevue/tag';

  defineProps<{
    role: RoleResponseDto;
  }>();
</script>
