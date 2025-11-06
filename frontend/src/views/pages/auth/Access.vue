// src/views/pages/auth/Access.vue
<script setup lang="ts">
  import { useRouter } from 'vue-router';
  import { useAuthStore } from '@/stores/authStore';

  const router = useRouter();
  const authStore = useAuthStore();

  const goBack = () => {
    router.back();
  };

  const goHome = () => {
    if (authStore.isSuperAdmin) {
      router.push({ name: 'superadmin-dashboard' });
    } else {
      router.push({ name: 'dashboard' });
    }
  };
</script>

<template>
  <div class="flex min-h-screen items-center justify-center px-6">
    <div class="text-center">
      <div class="mb-8">
        <i class="pi pi-lock text-8xl text-red-500"></i>
      </div>

      <h1 class="mb-4 text-6xl font-bold text-surface-900 dark:text-surface-0">Access Denied</h1>

      <p class="mb-8 text-xl text-surface-600 dark:text-surface-400">
        You don't have permission to access this page.
      </p>

      <div class="flex justify-center gap-4">
        <Button label="Go Back" icon="pi pi-arrow-left" @click="goBack" severity="secondary" />
        <Button label="Go to Dashboard" icon="pi pi-home" @click="goHome" />
      </div>
    </div>
  </div>
</template>
