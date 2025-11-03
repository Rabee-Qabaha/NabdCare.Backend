<script setup lang="ts">
import { ref, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useToast } from "primevue/usetoast";
import { useId } from "vue";
import { useAuthStore } from "@/stores/authStore";
import { getDefaultDashboardRoute } from "@/utils/navigation";

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();
const toast = useToast();

// 🔹 Form state
const email = ref("");
const password = ref("");
const rememberMe = ref(false);

// 🔹 Unique IDs (for accessibility)
const baseId = useId();
const ids = {
  email: `${baseId}-email`,
  password: `${baseId}-password`,
  remember: `${baseId}-remember`,
};

const isFormValid = computed(() => email.value.trim() && password.value.trim());

// 🔹 Handle login
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
      detail: `Hello, ${user.name || user.email}!`,
      life: 3000,
    });

    // ✅ Determine redirect target
    let redirectPath: string | null =
      (route.query.redirect as string | undefined) ??
      localStorage.getItem("lastVisitedRoute");

    // 🧠 If we have a redirect query, persist it for page reloads
    if (route.query.redirect) {
      localStorage.setItem(
        "redirectAfterLogin",
        route.query.redirect as string
      );
    }

    // 🧩 Recover saved redirect if we lost it (e.g., after refresh)
    if (!redirectPath) {
      redirectPath = localStorage.getItem("redirectAfterLogin");
    }

    if (redirectPath) {
      console.log("↪️ Redirecting to last route:", redirectPath);

      const normalizedRedirect = redirectPath.startsWith("/")
        ? redirectPath
        : `/${redirectPath}`;

      await router.isReady();
      await router.replace(normalizedRedirect);

      // ✅ Clean up URL (remove ?redirect=...)
      window.history.replaceState({}, "", normalizedRedirect);

      // 🧹 Cleanup stored redirects
      localStorage.removeItem("lastVisitedRoute");
      localStorage.removeItem("redirectAfterLogin");
    } else {
      console.log("🏠 Redirecting to default dashboard");
      const target = getDefaultDashboardRoute();
      await router.isReady();
      await router.replace(target);

      // ✅ Clean URL
      if (typeof target === "string") {
        window.history.replaceState({}, "", target);
      } else if ("name" in target) {
        const resolved = router.resolve(target);
        window.history.replaceState({}, "", resolved.fullPath);
      }
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
          <!-- Email -->
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

          <!-- Password -->
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

          <!-- Remember Me -->
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

          <!-- Submit -->
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

        <!-- Error message -->
        <Message
          v-if="authStore.error"
          severity="error"
          :closable="true"
          @close="authStore.clearError()"
          class="mb-4"
        >
          {{ authStore.error }}
        </Message>

        <!-- Footer -->
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
:deep(.p-password) {
  width: 100%;
}

:deep(.p-password input) {
  width: 100%;
}
</style>
