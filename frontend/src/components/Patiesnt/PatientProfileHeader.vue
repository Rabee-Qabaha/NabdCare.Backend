<template>
  <div class="card shadow-2 border-round overflow-hidden p-0">
    <!-- Cover Image -->
    <img
      src="/images/user-profile-header-bg.png"
      alt="Cover"
      class="w-full object-cover"
      style="height: 200px"
    />

    <!-- Profile Info -->
    <div
      class="relative flex flex-col items-start px-4 py-6 md:flex-row md:items-center md:px-6 md:py-10 md:pl-40"
    >
      <!-- Avatar -->
      <div
        class="border-3 border-round shadow-2 absolute -top-16 left-4 overflow-hidden border-white md:absolute md:left-6"
        style="width: 130px; height: 150px"
      >
        <img
          :src="
            patient?.gender === 'Female'
              ? '/images/default-Female_Avatar.avif'
              : '/images/default-Male_Avatar.avif'
          "
          alt="Profile"
          class="h-full w-full object-cover"
        />
      </div>

      <!-- Details -->
      <div class="mt-20 w-full flex-1 md:ml-8 md:mt-0">
        <h2 class="m-0 text-lg font-bold">{{ patient?.name }}</h2>
        <div class="text-500 mt-2 flex flex-col gap-2 text-sm sm:flex-row sm:flex-wrap">
          <div class="flex items-center gap-1">
            <i class="pi pi-phone"></i> {{ formatPhone(patient?.phone) }}
          </div>
          <div class="flex items-center gap-1">
            <i class="pi pi-map-marker"></i> {{ patient?.address }}
          </div>
          <div class="flex items-center gap-1">
            <i class="pi pi-calendar"></i> Joined
            {{ formatDate(patient?.createdAt) }}
          </div>
        </div>
      </div>

      <!-- Edit Button -->
      <div class="mt-4 w-full md:ml-auto md:mt-0 md:w-auto">
        <Button
          label="Edit profile"
          icon="pi pi-cog"
          severity="primary"
          class="w-full md:w-auto"
          @click="openEditDialog"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  // import type { Patient } from '@/../../shared/types';
  import { formatDate, formatPhone } from '@/utils/uiHelpers';
  import { toRefs } from 'vue';

  const props = defineProps<{
    patient: any | null; // Replace 'any' with 'Patient' when the type is available
    openEditDialog: () => void;
  }>();

  const { patient, openEditDialog } = toRefs(props);
</script>
