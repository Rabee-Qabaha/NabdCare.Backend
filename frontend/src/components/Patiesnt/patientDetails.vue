<template>
  <div class="card pl-4 pr-4">
    <!-- Header -->
    <div class="mb-4 ml-4 text-xl font-semibold text-muted-color">
      {{ header }}
    </div>

    <!-- Patient Details Card -->
    <div v-if="patient" class="space-y-4 rounded-lg pl-6 pr-6 pt-0">
      <!-- Name -->
      <h2 class="m-0 text-lg font-bold">{{ patient?.name }}</h2>

      <!-- Info List -->
      <div class="space-y-3 text-sm">
        <div class="flex items-center gap-3">
          <i class="pi pi-calendar text-base text-muted-color"></i>
          <span>{{ formatDate(patient?.dob) }}</span>
        </div>

        <div class="flex items-center gap-3">
          <i class="pi pi-user text-base text-muted-color"></i>
          <span>{{ patient?.gender }}</span>
        </div>

        <div class="flex items-center gap-3">
          <i class="pi pi-phone text-base text-muted-color"></i>
          <span>{{ formatPhone(patient?.phone) }}</span>
        </div>

        <div class="flex items-center gap-3">
          <i class="pi pi-map-marker text-base text-muted-color"></i>
          <span>{{ patient?.address }}</span>
        </div>

        <div class="flex items-center gap-3">
          <i class="pi pi-calendar text-base text-muted-color"></i>
          <span>Joined: {{ formatDate(patient?.createdAt) }}</span>
        </div>
      </div>

      <Divider class="my-4" />

      <!-- About Me Section -->
      <div class="flex items-center gap-2">
        <i class="pi pi-clipboard text-base text-muted-color"></i>
        <span class="font-medium text-muted-color">Note</span>
      </div>
      <ScrollPanel style="height: 4rem">
        <p class="text-sm leading-relaxed">
          {{ patient?.description || 'No description available.' }}
        </p>
      </ScrollPanel>
    </div>
  </div>
</template>

<script setup lang="ts">
  // import type { Patient } from "@/../../shared/types";
  import { formatDate, formatPhone } from '@/utils/uiHelpers';
  import { toRefs } from 'vue';

  const props = defineProps<{
    patient: any | null; // Replace 'any' with 'Patient' when the type is available
    header?: string;
  }>();

  const { patient, header } = toRefs(props);
</script>
