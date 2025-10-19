<!-- <script setup>
    import FloatingConfigurator from '@/components/FloatingConfigurator.vue';
</script>

<template>
    <FloatingConfigurator />
    <div class="bg-surface-50 dark:bg-surface-950 flex items-center justify-center min-h-screen min-w-[100vw] overflow-hidden">
        <div class="flex flex-col items-center justify-center">
            <div style="border-radius: 56px; padding: 0.3rem; background: linear-gradient(180deg, rgba(247, 149, 48, 0.4) 10%, rgba(247, 149, 48, 0) 30%)">
                <div class="w-full bg-surface-0 dark:bg-surface-900 py-20 px-8 sm:px-20 flex flex-col items-center" style="border-radius: 53px">
                    <div class="gap-4 flex flex-col items-center">
                        <div class="flex justify-center items-center border-2 border-orange-500 rounded-full" style="width: 3.2rem; height: 3.2rem">
                            <i class="text-orange-500 pi pi-fw pi-lock !text-xxl"></i>
                        </div>
                        <h1 class="text-surface-900 dark:text-surface-0 font-bold text-4xl lg:text-5xl mb-2">Access Denied</h1>
                        <span class="text-muted-color mb-8">You do not have the necessary permisions. Please contact admins.</span>
                        <img src="/demo/images/access/asset-access.svg" alt="Access denied" class="mb-8" width="80%" />
                        <div class="col-span-12 mt-8 text-center">
                            <Button as="router-link" label="Go to Dashboard" to="/" severity="warn" />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template> -->

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
