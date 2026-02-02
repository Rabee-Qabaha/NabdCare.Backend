<template>
  <BaseCard
    no-padding
    class="relative !flex-col md:!flex-row md:!items-center p-6 group"
    :class="{ 'opacity-70 grayscale !bg-surface-50 dark:!bg-surface-900/50': !branch.isActive }"
  >
    <!-- Left: Icon & Main Info -->
    <div class="flex items-start gap-5 flex-1 w-full">
      <!-- Icon Box -->
      <div
        class="w-14 h-14 rounded-xl flex items-center justify-center shrink-0 transition-colors"
        :class="[
          branch.isMain
            ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400'
            : 'bg-surface-100 dark:bg-surface-800 text-surface-500 dark:text-surface-400',
        ]"
      >
        <i class="pi text-2xl" :class="branch.isMain ? 'pi-building' : 'pi-home'"></i>
      </div>

      <!-- Content -->
      <div class="space-y-3 w-full">
        <!-- Header -->
        <div class="flex flex-wrap items-center gap-2">
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-none">
            {{ branch.name }}
          </h3>
          <Tag
            v-if="branch.isMain"
            value="MAIN BRANCH"
            severity="info"
            class="!text-[10px] !font-bold !px-2 !py-0.5 !h-5 uppercase tracking-wide"
          />
          <Tag
            v-if="!branch.isActive"
            value="INACTIVE"
            severity="danger"
            class="!text-[10px] !font-bold !px-2 !py-0.5 !h-5 uppercase tracking-wide"
          />
        </div>

        <!-- Details Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-3 text-sm mt-3">
          <!-- Column 1: Address -->
          <div class="flex items-start gap-2.5 text-surface-600 dark:text-surface-300">
            <i class="pi pi-map-marker text-surface-400 mt-0.5 shrink-0"></i>
            <span class="leading-snug">{{ branch.address || 'No address provided' }}</span>
          </div>

          <!-- Column 2: Contact Info (Email & Phone) -->
          <div class="flex flex-col gap-2">
            <!-- Email -->
            <div
              v-if="branch.email"
              class="flex items-center gap-2.5 text-surface-600 dark:text-surface-300"
            >
              <i class="pi pi-envelope text-surface-400 shrink-0"></i>
              <span class="truncate">{{ branch.email }}</span>
            </div>

            <!-- Phone -->
            <div class="flex items-center gap-2.5 text-surface-600 dark:text-surface-300">
              <i class="pi pi-phone text-surface-400 shrink-0"></i>
              <span>{{ branch.phone || '-' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Right: Actions & Status -->
    <div
      class="mt-6 md:mt-0 flex md:flex-col items-center md:items-end justify-between w-full md:w-auto md:pl-6 md:border-l border-surface-100 dark:border-surface-800 gap-4"
    >
      <!-- Active Switch -->
      <div class="flex items-center gap-3">
        <span
          class="text-xs font-semibold text-surface-400 uppercase tracking-wider hidden md:block"
        >
          Active Status
        </span>
        <ToggleSwitch
          :model-value="branch.isActive"
          @update:model-value="emit('toggle-status', branch)"
          :disabled="isToggling"
        />
      </div>

      <!-- Action Buttons -->
      <div class="flex gap-1 transition-opacity">
        <Button
          icon="pi pi-pencil"
          text
          rounded
          severity="secondary"
          aria-label="Edit"
          v-tooltip.top="'Edit Details'"
          @click="emit('edit', branch)"
        />
        <Button
          icon="pi pi-trash"
          text
          rounded
          severity="danger"
          aria-label="Delete"
          v-tooltip.top="'Delete Branch'"
          :disabled="branch.isMain"
          @click="emit('delete', branch)"
        />
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue'; // Correct path for BaseCard
  import type { BranchResponseDto } from '@/types/backend';
  import Button from 'primevue/button';
  import Tag from 'primevue/tag';
  import ToggleSwitch from 'primevue/toggleswitch';

  defineProps<{
    branch: BranchResponseDto;
    isToggling?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'edit', branch: BranchResponseDto): void;
    (e: 'toggle-status', branch: BranchResponseDto): void;
    (e: 'delete', branch: BranchResponseDto): void;
  }>();
</script>
