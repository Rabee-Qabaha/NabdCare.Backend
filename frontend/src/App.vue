// src/App.vue
<script setup lang="ts">
import { computed } from "vue";
import { useAuthStore } from "@/stores/authStore";
import { VueQueryDevtools } from "@tanstack/vue-query-devtools";
import Toast from "primevue/toast";
import { useToast } from "primevue/usetoast";
import { setToastInstance } from "./service/toastService";

const authStore = useAuthStore();

// ✅ define once — Vue compiler can’t handle `import.meta` in template
const isDev = import.meta.env.DEV;

// 🧠 Compute app mode label
const appMode = computed(() => {
  if (!authStore.isLoggedIn) return "Guest";
  if (authStore.isSuperAdmin) return "SuperAdmin";
  const clinic = authStore.currentUser?.clinicId || "ClinicId";
  const role = authStore.currentUser?.role || "User";
  return `${role} @ ${clinic}`;
});

const toast = useToast();
setToastInstance(toast);
</script>

<template>
  <Toast />
  <router-view />

  <!-- ✅ Safe usage -->
  <VueQueryDevtools v-if="isDev" position="bottom-right" />

  <!-- 🧩 Custom app info overlay -->
  <div
    v-if="isDev"
    class="fixed bottom-16 right-4 bg-gray-800/80 text-white text-xs px-3 py-1.5 rounded-lg shadow-lg backdrop-blur-sm"
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
