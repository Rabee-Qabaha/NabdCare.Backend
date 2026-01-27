<script setup lang="ts">
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import type { UserResponseDto } from '@/types/backend';
  import { formatUserDisplay } from '@/utils/users/userDisplay';
  import Avatar from 'primevue/avatar';
  import Tag from 'primevue/tag';
  import { useToast } from 'primevue/usetoast';
  import { computed } from 'vue';

  const props = defineProps<{
    visible: boolean;
    user: UserResponseDto;
  }>();

  const emit = defineEmits(['update:visible']);
  const toast = useToast();

  const display = computed(() => formatUserDisplay(props.user));
  const initials = computed(() => display.value.initials);

  const avatarClass = computed(() => {
    if (props.user.isDeleted) return 'bg-surface-300 grayscale opacity-70';
    if (!props.user.isActive) return 'bg-orange-400';
    return 'bg-primary-500';
  });

  const roleTagStyle = computed(() => {
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

  const timeAgo = (date: Date | string | undefined) => {
    if (!date) return 'Never';
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

  function formatDate(date: string | Date) {
    if (!date) return '-';
    return new Date(date).toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  function copyToClipboard(text: string) {
    if (!text) return;
    navigator.clipboard.writeText(text);
    toast.add({ severity: 'secondary', summary: 'Copied', detail: text, life: 1500 });
  }
</script>

<template>
  <BaseDrawer
    :visible="visible"
    width="md:!w-[450px]"
    :title="`${user.fullName} Details`"
    subtitle="Full user profile details and information"
    icon="pi pi-user"
    :dismissable="true"
    @update:visible="emit('update:visible', $event)"
    @close="emit('update:visible', false)"
  >
    <div class="flex flex-col h-full gap-6">
      <!-- HEADER (Centered Avatar) -->
      <div class="flex flex-col items-center text-center pb-2 pt-2">
        <div class="relative inline-block mb-4">
          <Avatar
            :image="user.profilePictureUrl"
            :label="!user.profilePictureUrl ? initials : undefined"
            size="xlarge"
            shape="circle"
            class="!w-24 !h-24 !text-3xl font-bold shadow-lg"
            :class="avatarClass"
          />
          <span
            class="absolute bottom-1 right-1 block w-5 h-5 rounded-full ring-4 ring-white dark:ring-surface-900"
            :class="user.isActive && !user.isDeleted ? 'bg-green-500' : 'bg-surface-400'"
          ></span>
        </div>

        <!-- Name & Job Title -->
        <h2 class="text-2xl font-bold text-surface-900 dark:text-surface-0 mb-1">
          {{ user.fullName }}
        </h2>

        <div class="text-primary-600 dark:text-primary-400 font-semibold mb-3">
          {{ user.jobTitle || 'Staff Member' }}
        </div>

        <div class="flex flex-wrap justify-center gap-2 mb-4">
          <div
            class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full border text-xs font-bold uppercase tracking-wide"
            :style="roleTagStyle"
          >
            <i v-if="user.roleIcon" :class="user.roleIcon"></i>
            <i v-else class="pi pi-shield"></i>
            {{ user.roleName }}
          </div>
          <Tag v-if="user.isSystemRole" value="System" severity="info" />
          <Tag v-if="user.isDeleted" value="Deleted" severity="danger" />
          <Tag v-else-if="!user.isActive" value="Inactive" severity="warn" />
        </div>

        <div class="flex gap-4 text-xs text-surface-500">
          <span class="flex items-center gap-1.5" title="Joined Date">
            <i class="pi pi-calendar"></i>
            Joined {{ formatDate(user.createdAt).split(',')[0] }}
          </span>
          <span class="flex items-center gap-1.5" title="Last Login">
            <i class="pi pi-history"></i>
            Login: {{ timeAgo(user.lastLoginAt) }}
          </span>
        </div>
      </div>

      <div class="w-full h-px bg-surface-200 dark:bg-surface-700"></div>

      <!-- Contact Info -->
      <section>
        <h3
          class="text-sm font-bold text-surface-900 dark:text-surface-0 mb-3 flex items-center gap-2"
        >
          <i class="pi pi-id-card text-primary-500"></i>
          Contact Information
        </h3>
        <div class="bg-surface-50 dark:bg-surface-800/50 rounded-xl p-4 flex flex-col gap-3">
          <div
            class="flex items-center justify-between group cursor-pointer"
            @click="copyToClipboard(user.email)"
          >
            <div class="flex items-center gap-3">
              <div
                class="w-8 h-8 rounded-full bg-white dark:bg-surface-800 flex items-center justify-center text-surface-500 shadow-sm"
              >
                <i class="pi pi-envelope"></i>
              </div>
              <div class="flex flex-col">
                <span class="text-xs text-surface-500">Email Address</span>
                <span class="text-sm font-medium text-surface-900 dark:text-surface-0">
                  {{ user.email }}
                </span>
              </div>
            </div>
            <i
              class="pi pi-copy text-surface-400 opacity-0 group-hover:opacity-100 transition-opacity"
            ></i>
          </div>

          <div v-if="user.phoneNumber" class="flex items-center gap-3">
            <div
              class="w-8 h-8 rounded-full bg-white dark:bg-surface-800 flex items-center justify-center text-surface-500 shadow-sm"
            >
              <i class="pi pi-phone"></i>
            </div>
            <div class="flex flex-col">
              <span class="text-xs text-surface-500">Phone Number</span>
              <span class="text-sm font-medium text-surface-900 dark:text-surface-0">
                {{ user.phoneNumber }}
              </span>
            </div>
          </div>

          <div v-if="user.address" class="flex items-center gap-3">
            <div
              class="w-8 h-8 rounded-full bg-white dark:bg-surface-800 flex items-center justify-center text-surface-500 shadow-sm"
            >
              <i class="pi pi-map-marker"></i>
            </div>
            <div class="flex flex-col">
              <span class="text-xs text-surface-500">Address</span>
              <span class="text-sm font-medium text-surface-900 dark:text-surface-0">
                {{ user.address }}
              </span>
            </div>
          </div>
        </div>
      </section>

      <!-- Professional Info -->
      <section>
        <h3
          class="text-sm font-bold text-surface-900 dark:text-surface-0 mb-3 flex items-center gap-2"
        >
          <i class="pi pi-briefcase text-primary-500"></i>
          Professional Details
        </h3>
        <div class="grid grid-cols-2 gap-3">
          <div class="bg-surface-50 dark:bg-surface-800/50 rounded-xl p-3">
            <span class="text-xs text-surface-500 block mb-1">Clinic</span>
            <span
              class="text-sm font-bold text-surface-900 dark:text-surface-0 block truncate"
              :title="user.clinicName"
            >
              {{ user.clinicName || 'Global' }}
            </span>
          </div>
          <div class="bg-surface-50 dark:bg-surface-800/50 rounded-xl p-3">
            <span class="text-xs text-surface-500 block mb-1">License No.</span>
            <span class="text-sm font-bold text-surface-900 dark:text-surface-0 block truncate">
              {{ user.licenseNumber || 'N/A' }}
            </span>
          </div>
        </div>
      </section>

      <!-- Bio Info -->
      <section v-if="user.bio">
        <h3
          class="text-sm font-bold text-surface-900 dark:text-surface-0 mb-3 flex items-center gap-2"
        >
          <i class="pi pi-user text-primary-500"></i>
          Bio
        </h3>
        <div class="bg-surface-50 dark:bg-surface-800/50 rounded-xl p-4">
          <p class="text-sm text-surface-700 dark:text-surface-200 leading-relaxed italic">
            "{{ user.bio }}"
          </p>
        </div>
      </section>

      <!-- System Metadata -->
      <section>
        <h3
          class="text-sm font-bold text-surface-900 dark:text-surface-0 mb-3 flex items-center gap-2"
        >
          <i class="pi pi-cog text-primary-500"></i>
          System Information
        </h3>
        <div
          class="text-xs text-surface-500 space-y-2 bg-surface-50 dark:bg-surface-800/50 rounded-xl p-4"
        >
          <div class="flex justify-between items-center group">
            <span>User ID</span>
            <span
              class="font-mono text-surface-900 dark:text-surface-0 select-all cursor-copy"
              @click="copyToClipboard(user.id)"
              v-tooltip="'Click to copy'"
            >
              {{ user.id }}
            </span>
          </div>
          <div class="flex justify-between items-center group" v-if="user.clinicId">
            <span>Clinic ID</span>
            <span
              class="font-mono text-surface-900 dark:text-surface-0 select-all cursor-copy"
              @click="copyToClipboard(user.clinicId)"
              v-tooltip="'Click to copy'"
            >
              {{ user.clinicId }}
            </span>
          </div>
          <div class="flex justify-between items-center group">
            <span>Role ID</span>
            <span
              class="font-mono text-surface-900 dark:text-surface-0 select-all cursor-copy"
              @click="copyToClipboard(user.roleId)"
              v-tooltip="'Click to copy'"
            >
              {{ user.roleId }}
            </span>
          </div>
          <div class="flex justify-between">
            <span>Created By</span>
            <span class="text-surface-900 dark:text-surface-0">
              {{ user.createdByUserName || 'System' }}
            </span>
          </div>
          <div class="flex justify-between">
            <span>Created At</span>
            <span class="text-surface-900 dark:text-surface-0">
              {{ formatDate(user.createdAt) }}
            </span>
          </div>
          <div class="flex justify-between">
            <span>Last Update</span>
            <span class="text-surface-900 dark:text-surface-0">
              {{ formatDate(user.updatedAt) }}
            </span>
          </div>
        </div>
      </section>
    </div>
    <template #footer="{ close }">
      <Button label="Close" outlined severity="secondary" @click="close" class="w-full" />
    </template>
  </BaseDrawer>
</template>
