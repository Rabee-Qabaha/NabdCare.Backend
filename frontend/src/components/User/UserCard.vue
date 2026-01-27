// src/components/User/UserCard.vue
<template>
  <BaseCard no-padding class="relative">
    <div v-if="user.isDeleted" class="absolute top-0 left-0 w-full h-1 bg-red-500 z-10"></div>

    <!-- LAST LOGIN (Top Left) -->
    <div
      class="absolute top-3 left-3 z-20 flex items-center gap-1.5 px-2 py-1 rounded bg-surface-100 dark:bg-surface-800 text-[9px] font-bold text-surface-500 shadow-sm"
      title="Last time the user logged in"
    >
      <i class="pi pi-history text-[8px]"></i>
      <span>{{ user.lastLoginAt ? timeAgo(user.lastLoginAt) : 'Never' }}</span>
    </div>

    <div class="absolute top-3 right-3 z-20 flex items-center gap-2 pt-2">
      <Checkbox v-if="!user.isDeleted" v-model="selectedIds" :value="user.id" />

      <span
        v-if="user.isDeleted"
        class="text-[10px] font-bold text-red-600 dark:text-red-400 uppercase bg-red-100 dark:bg-red-900/30 px-2 py-0.5 rounded"
      >
        Deleted
      </span>
    </div>

    <div class="p-5 flex flex-col items-center text-center relative mt-2">
      <div class="relative inline-block mb-3">
        <Avatar
          :label="initials"
          size="xlarge"
          shape="circle"
          class="shadow-md font-bold text-white text-xl"
          :class="avatarClass"
        />
        <span
          class="absolute bottom-0.5 right-0.5 block h-3.5 w-3.5 rounded-full ring-2 ring-white dark:ring-surface-900"
          :class="user.isActive && !user.isDeleted ? 'bg-green-500' : 'bg-surface-400'"
        ></span>
      </div>

      <h3
        class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight px-4 w-full truncate"
        :title="user.fullName"
      >
        {{ user.fullName }}
      </h3>

      <div
        class="text-xs font-semibold text-primary-600 dark:text-primary-400 mt-1 truncate w-full px-4"
        :title="user.jobTitle"
      >
        {{ user.jobTitle || 'Staff Member' }}
      </div>

      <div class="flex flex-col gap-1 items-center justify-center mt-2 w-full px-2">
        <div
          class="relative flex justify-center group cursor-pointer"
          @click.stop="copyToClipboard(user.email)"
        >
          <span
            class="text-sm text-surface-500 dark:text-surface-400 truncate max-w-[180px] group-hover:text-primary-600 transition-colors text-center"
            :title="user.email"
          >
            {{ user.email }}
          </span>
          <!-- Absolute positioned icon to not affect centering -->
          <i
            class="absolute -right-5 top-1/2 -translate-y-1/2 pi pi-copy text-xs text-surface-400 opacity-0 group-hover:opacity-100 transition-opacity"
          ></i>
        </div>
      </div>

      <div class="flex flex-wrap justify-center gap-2 mt-3">
        <div
          class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded border text-[10px] font-bold uppercase tracking-wide"
          :style="roleTagStyle"
        >
          <i v-if="user.roleIcon" :class="user.roleIcon"></i>
          <i v-else class="pi pi-id-card"></i>
          {{ user.roleName }}
        </div>

        <Tag
          v-if="!user.isActive && !user.isDeleted"
          value="Inactive"
          severity="warn"
          class="!text-[10px]"
        />
      </div>
    </div>

    <div class="px-5 pb-4">
      <div
        class="group relative overflow-hidden rounded-lg border border-surface-100 dark:border-surface-700 p-3 flex flex-col gap-2"
        :class="
          user.isDeleted
            ? 'bg-white/50 dark:bg-surface-800/30'
            : 'bg-surface-50 dark:bg-surface-800/50'
        "
      >
        <!-- Hover Overlay -->
        <div
          class="absolute inset-0 bg-white/80 dark:bg-surface-900/80 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity z-10 cursor-pointer"
          @click="drawerVisible = true"
        >
          <Button
            label="View Full Profile"
            size="small"
            rounded
            outlined
            class="text-xs !bg-white dark:!bg-surface-800"
            @click.stop="drawerVisible = true"
          />
        </div>
        <div class="flex items-center justify-between text-xs">
          <span class="text-surface-500 flex items-center gap-1.5">
            <i class="pi pi-building"></i>
            Clinic
          </span>
          <span
            class="font-medium text-surface-800 dark:text-surface-200 truncate max-w-[120px]"
            :title="user.clinicName"
          >
            {{ user.clinicName || 'Global' }}
          </span>
        </div>
        <div class="flex items-center justify-between text-xs">
          <span class="text-surface-500 flex items-center gap-1.5">
            <i class="pi pi-phone"></i>
            Phone
          </span>
          <span class="font-medium text-surface-800 dark:text-surface-200 truncate max-w-[120px]">
            {{ user.phoneNumber || 'No phone number' }}
          </span>
        </div>
        <div class="flex items-center justify-between text-xs">
          <span class="text-surface-500 flex items-center gap-1.5">
            <i class="pi pi-user-plus"></i>
            Added By
          </span>
          <span class="font-medium text-surface-800 dark:text-surface-200 truncate max-w-[120px]">
            {{ user.createdByUserName || 'System' }}
          </span>
        </div>
        <div class="flex items-center justify-between text-xs">
          <span class="text-surface-500 flex items-center gap-1.5">
            <i class="pi pi-calendar"></i>
            Joined
          </span>
          <span class="font-medium text-surface-800 dark:text-surface-200">
            {{ formatDate(user.createdAt) }}
          </span>
        </div>
      </div>
    </div>

    <div
      class="mt-auto flex items-center justify-between p-3 border-t rounded-b-xl"
      :class="[
        user.isDeleted
          ? 'border-red-100 dark:border-red-900/30 bg-white/60 dark:bg-surface-900/60'
          : 'border-surface-100 dark:border-surface-700 bg-white dark:bg-surface-900',
      ]"
    >
      <span
        v-tooltip.top="'Copy ID'"
        class="text-[9px] font-mono text-surface-300 dark:text-surface-600 cursor-pointer hover:text-primary-500 transition-colors"
        @click.stop="copyToClipboard(user.id)"
      >
        #{{ user.id.slice(0, 8) }}
      </span>

      <div class="flex gap-1">
        <slot name="actions"></slot>
      </div>
    </div>

    <UserDetailsDrawer v-model:visible="drawerVisible" :user="user" />
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import type { UserResponseDto } from '@/types/backend';
  import { formatUserDisplay } from '@/utils/users/userDisplay';
  import Avatar from 'primevue/avatar';
  import Button from 'primevue/button';
  import Checkbox from 'primevue/checkbox';
  import Tag from 'primevue/tag';
  import { useToast } from 'primevue/usetoast';
  import { computed, ref } from 'vue';
  import UserDetailsDrawer from './UserDetailsDrawer.vue';

  const props = defineProps<{
    user: UserResponseDto;
  }>();

  const selectedIds = defineModel<string[]>('selected');
  const toast = useToast();
  const drawerVisible = ref(false);

  const display = computed(() => formatUserDisplay(props.user));
  const initials = computed(() => display.value.initials);

  const timeAgo = (date: Date | string | undefined) => {
    if (!date) return '';
    const now = new Date();
    const diff = now.getTime() - new Date(date).getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days}d ago`;
    if (hours > 0) return `${hours}h ago`;
    if (minutes > 0) return `${minutes}m ago`;
    return 'Just now';
  };

  const avatarClass = computed(() => {
    // Kept the grayscale for deleted users as it fits the "Deleted" look well
    if (props.user.isDeleted) return 'bg-surface-300 grayscale opacity-70';
    if (!props.user.isActive) return 'bg-orange-400';
    return 'bg-primary-500';
  });

  const roleTagStyle = computed(() => {
    // If deleted, we desaturate the role tag slightly to match the theme
    const hex = props.user.roleColorCode || '#64748b';
    if (props.user.isDeleted) {
      return {
        color: '#64748b',
        borderColor: '#e2e8f0',
        backgroundColor: '#f8fafc',
      };
    }
    return {
      color: hex,
      borderColor: hex,
      backgroundColor: `color-mix(in srgb, ${hex} 10%, transparent)`,
    };
  });

  function formatDate(date: string | Date) {
    if (!date) return '-';
    return new Date(date).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  function copyToClipboard(text: string) {
    navigator.clipboard.writeText(text);
    toast.add({ severity: 'secondary', summary: 'Copied', detail: text, life: 1500 });
  }
</script>
