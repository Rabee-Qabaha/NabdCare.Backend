// src/views/pages/auth/Login.vue
<script setup lang="ts">
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import { useId } from "vue";
import { useAuthStore } from "@/stores/authStore";

const authStore = useAuthStore();
const router = useRouter();
const toast = useToast();

// Form state
const email = ref("");
const password = ref("");
const rememberMe = ref(false);

// Unique IDs
const baseId = useId();
const ids = {
  email: `${baseId}-email`,
  password: `${baseId}-password`,
  remember: `${baseId}-remember`,
};

const isFormValid = computed(() => email.value.trim() && password.value.trim());

const handleLogin = async (): Promise<void> => {
  if (!isFormValid.value) return;

  try {
    const user = await authStore.login(
      email.value,
      password.value,
      rememberMe.value
    );

    toast.add({
      severity: "success",
      summary: "Welcome Back",
      detail: `Hello, ${user.fullName}!`,
      life: 3000,
    });

    // Redirect automatically based on role
    if (authStore.isSuperAdmin) {
      router.push({ name: "superadmin-dashboard" });
    } else {
      router.push({ name: "dashboard" });
    }
  } catch (err: any) {
    toast.add({
      severity: "error",
      summary: "Login Failed",
      detail: err.message || "Invalid email or password",
      life: 5000,
    });
  }
};

const handleKeyPress = (event: KeyboardEvent): void => {
  if (event.key === "Enter" && isFormValid.value) handleLogin();
};
</script>

<template>
  <div class="flex min-h-screen">
    <!-- Left Side - Illustration -->
    <div
      class="hidden lg:flex w-1/2 bg-surface-50 dark:bg-surface-950 items-center justify-center p-8"
    >
      <div class="max-w-lg">
        <img
          src="/images/Log-in.avif"
          alt="Login illustration"
          class="w-full h-auto"
        />
      </div>
    </div>

    <!-- Right Side - Login Form -->
    <div
      class="flex flex-col justify-center items-center w-full lg:w-1/2 bg-white dark:bg-surface-900 px-6 sm:px-12"
    >
      <div class="w-full max-w-md">
        <!-- Header -->
        <div class="mb-8">
          <h1
            class="text-3xl font-bold text-surface-900 dark:text-surface-0 mb-2"
          >
            Welcome to NabdCare! 👋
          </h1>
          <p class="text-surface-600 dark:text-surface-400">
            Please sign in to your account to continue
          </p>
        </div>

        <!-- Login Form -->
        <form @submit.prevent="handleLogin" @keypress="handleKeyPress">
          <!-- Email Input -->
          <div class="mb-6">
            <label
              :for="ids.email"
              class="block text-surface-900 dark:text-surface-0 text-sm font-medium mb-2"
            >
              Email Address
            </label>
            <InputText
              :id="ids.email"
              v-model.trim="email"
              type="email"
              placeholder="your.email@example.com"
              class="w-full"
              required
              autocomplete="username"
              :disabled="authStore.loading"
              aria-label="Email address"
            />
          </div>

          <!-- Password Input -->
          <div class="mb-4">
            <label
              :for="ids.password"
              class="block text-surface-900 dark:text-surface-0 text-sm font-medium mb-2"
            >
              Password
            </label>
            <Password
              :id="ids.password"
              v-model="password"
              placeholder="Enter your password"
              :toggleMask="true"
              :feedback="false"
              class="w-full"
              inputClass="w-full"
              required
              autocomplete="current-password"
              :disabled="authStore.loading"
              aria-label="Password"
            />
          </div>

          <!-- Remember Me Checkbox -->
          <div class="flex items-center justify-between mb-6">
            <div class="flex items-center">
              <Checkbox
                v-model="rememberMe"
                :inputId="ids.remember"
                :binary="true"
                class="mr-2"
                :disabled="authStore.loading"
              />
              <label
                :for="ids.remember"
                class="text-sm text-surface-700 dark:text-surface-300 cursor-pointer select-none"
              >
                Remember me
              </label>
            </div>
          </div>

          <!-- Submit Button -->
          <Button
            type="submit"
            label="Sign In"
            icon="pi pi-sign-in"
            class="w-full mb-6"
            :loading="authStore.loading"
            :disabled="authStore.loading || !isFormValid"
            severity="primary"
          />
        </form>

        <!-- Error Message -->
        <Message
          v-if="authStore.error"
          severity="error"
          :closable="true"
          @close="authStore.clearError()"
          class="mb-4"
        >
          {{ authStore.error }}
        </Message>

        <!-- Additional Info -->
        <div
          class="text-center text-sm text-surface-600 dark:text-surface-400 mt-8"
        >
          <p>
            By signing in, you agree to our Terms of Service and Privacy Policy
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Ensure password input takes full width */
:deep(.p-password) {
  width: 100%;
}

:deep(.p-password input) {
  width: 100%;
}
</style>
