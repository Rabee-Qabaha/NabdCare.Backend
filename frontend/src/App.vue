// src/App.vue
<script setup lang="ts">
  import { useAuthStore } from '@/stores/authStore';
  import { VueQueryDevtools } from '@tanstack/vue-query-devtools';
  import Toast from 'primevue/toast';
  import { computed } from 'vue';

  const authStore = useAuthStore();

  // ✅ define once — Vue compiler can’t handle `import.meta` in template
  const isDev = import.meta.env.DEV;

  // 🧠 Compute app mode label
  const appMode = computed(() => {
    if (!authStore.isLoggedIn) return 'Guest';
    if (authStore.isSuperAdmin) return 'SuperAdmin';
    const clinic = authStore.currentUser?.clinicId || 'ClinicId';
    const role = authStore.currentUser?.roleName || 'User';
    return `${role} @ ${clinic}`;
  });
</script>

<template>
  <Toast />
  <router-view />

  <!-- ✅ Safe usage -->
  <VueQueryDevtools v-if="isDev" position="bottom-right" />

  <!-- 🧩 Custom app info overlay -->
  <div
    v-if="isDev"
    class="fixed bottom-16 right-4 rounded-lg bg-gray-800/80 px-3 py-1.5 text-xs text-white shadow-lg backdrop-blur-sm"
  >
    🧠 NabdCare – {{ appMode }}
  </div>
</template>

<style scoped>
  /* Optional: subtle fade-in for dev overlay */
  div {
    transition: opacity 0.3s ease;
  }
</style>
