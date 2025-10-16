<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import { useAuthStore } from "@/stores/authStore";

const router = useRouter();
const toast = useToast();
const authStore = useAuthStore();

const email = ref("");
const password = ref("");
const rememberMe = ref(false);

onMounted(() => {
  if (authStore.isLoggedIn) {
    const route = authStore.isSuperAdmin ? "/admin/dashboard" : "/dashboard";
    router.push(route);
  }
});

const handleLogin = async () => {
  try {
    await authStore.loginUser(email.value, password.value);
    toast.add({
      severity: "success",
      summary: "Welcome",
      detail: `Hello, ${email.value}`,
      life: 3000,
    });
    const route = authStore.isSuperAdmin ? "/admin/dashboard" : "/dashboard";
    router.push(route);
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Login Failed",
      detail: err.message || "Invalid email or password",
      life: 3000,
    });
  }
};
</script>

<template>
  <div class="flex min-h-screen">
    <!-- Illustration Left -->
    <div
      class="hidden lg:flex w-1/2 bg-surface-50 dark:bg-surface-950 items-center justify-center"
    >
      <img src="/images/Log-in.avif" class="mx-auto w-[36rem]" />
    </div>

    <!-- Form Right -->
    <div
      class="flex flex-col justify-center items-center w-full lg:w-1/2 bg-white dark:bg-surface-900 px-6 sm:px-12"
    >
      <div class="max-w-md w-full">
        <h2
          class="text-2xl font-semibold text-surface-900 dark:text-surface-0 mb-2"
        >
          Welcome to Patients Manager! 👋
        </h2>
        <p class="text-muted-color mb-8">Please sign-in to your account</p>

        <form @submit.prevent="handleLogin">
          <div class="mb-6">
            <label
              for="email"
              class="block text-surface-900 dark:text-surface-0 text-sm font-medium mb-2"
              >Email</label
            >
            <InputText
              v-model.trim="email"
              placeholder="Enter your email"
              class="w-full"
              required
              autocomplete="username"
            />
          </div>

          <div class="mb-4">
            <label
              for="password"
              class="block text-surface-900 dark:text-surface-0 text-sm font-medium mb-2"
              >Password</label
            >
            <Password
              v-model="password"
              placeholder="Password"
              :toggleMask="true"
              class="w-full"
              fluid
              :feedback="false"
              required
              autocomplete="current-password"
            />
          </div>

          <div class="flex items-center justify-between mb-6">
            <Checkbox v-model="rememberMe" binary class="mr-2" />
            <label class="text-sm">Remember Me</label>
          </div>

          <Button
            type="submit"
            label="Sign in"
            class="w-full mb-6"
            :disabled="authStore.loading"
          />
        </form>
      </div>
    </div>
  </div>
</template>
