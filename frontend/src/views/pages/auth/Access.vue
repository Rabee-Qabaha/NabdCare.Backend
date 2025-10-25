// src/views/pages/auth/Access.vue
<script setup lang="ts">
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/authStore";

const router = useRouter();
const authStore = useAuthStore();

const goBack = () => {
  router.back();
};

const goHome = () => {
  if (authStore.isSuperAdmin) {
    router.push({ name: "superadmin-dashboard" });
  } else {
    router.push({ name: "dashboard" });
  }
};
</script>

<template>
  <div class="flex items-center justify-center min-h-screen px-6">
    <div class="text-center">
      <div class="mb-8">
        <i class="pi pi-lock text-8xl text-red-500"></i>
      </div>

      <h1 class="text-6xl font-bold text-surface-900 dark:text-surface-0 mb-4">
        Access Denied
      </h1>

      <p class="text-xl text-surface-600 dark:text-surface-400 mb-8">
        You don't have permission to access this page.
      </p>

      <div class="flex gap-4 justify-center">
        <Button
          label="Go Back"
          icon="pi pi-arrow-left"
          @click="goBack"
          severity="secondary"
        />
        <Button label="Go to Dashboard" icon="pi pi-home" @click="goHome" />
      </div>
    </div>
  </div>
</template>
