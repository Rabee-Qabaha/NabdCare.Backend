<script setup lang="ts">
  import { useErrorHandler } from '@/composables/errorHandling/useErrorHandler';
  import { useAuthStore } from '@/stores/authStore';
  import { getDefaultDashboardRoute } from '@/utils/navigation';
  import { useToast } from 'primevue/usetoast';
  import { computed, ref, useId } from 'vue';
  import { useRoute, useRouter } from 'vue-router';

  // PrimeVue Components (Ensure these are registered globally or import them here)
  import Button from 'primevue/button';
  import Checkbox from 'primevue/checkbox';
  import FloatLabel from 'primevue/floatlabel';
  import IconField from 'primevue/iconfield';
  import InputIcon from 'primevue/inputicon';
  import InputText from 'primevue/inputtext';
  import Message from 'primevue/message';
  import Password from 'primevue/password';

  const authStore = useAuthStore();
  const router = useRouter();
  const route = useRoute();
  const toast = useToast();
  const { handleErrorAndNotify } = useErrorHandler();

  const email = ref('');
  const password = ref('');
  const rememberMe = ref(false);

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
      const user = await authStore.login(email.value, password.value, rememberMe.value);

      toast.add({
        severity: 'success',
        summary: 'Welcome Back',
        detail: `Hello, ${user.fullName || user.email}!`,
        life: 3000,
      });

      let redirectPath: string | null =
        (route.query.redirect as string | undefined) ?? localStorage.getItem('lastVisitedRoute');

      if (route.query.redirect) {
        localStorage.setItem('redirectAfterLogin', route.query.redirect as string);
      }

      if (!redirectPath) {
        redirectPath = localStorage.getItem('redirectAfterLogin');
      }

      if (redirectPath) {
        // ... (Existing redirect logic preserved)
        const normalizedRedirect = redirectPath.startsWith('/') ? redirectPath : `/${redirectPath}`;
        await router.isReady();
        await router.replace(normalizedRedirect);
        window.history.replaceState({}, '', normalizedRedirect);
        localStorage.removeItem('lastVisitedRoute');
        localStorage.removeItem('redirectAfterLogin');
      } else {
        const target = getDefaultDashboardRoute();
        await router.isReady();
        await router.replace(target);

        if (typeof target === 'string') {
          window.history.replaceState({}, '', target);
        } else if ('name' in target) {
          const resolved = router.resolve(target);
          window.history.replaceState({}, '', resolved.fullPath);
        }
      }
    } catch (error) {
      await handleErrorAndNotify(error);
    }
  };

  const handleKeyPress = (event: KeyboardEvent): void => {
    if (event.key === 'Enter' && isFormValid.value) handleLogin();
  };
</script>

<template>
  <div class="flex min-h-screen overflow-hidden bg-surface-0 dark:bg-surface-950">
    <div
      class="relative hidden w-1/2 flex-col items-center justify-center overflow-hidden bg-primary-50 p-12 lg:flex dark:bg-primary-950/30"
    >
      <div
        class="absolute inset-0 opacity-10"
        style="
          background-image: radial-gradient(#6366f1 1px, transparent 1px);
          background-size: 32px 32px;
        "
      ></div>

      <div class="relative z-10 max-w-lg text-center">
        <div class="animate-float mb-8">
          <img
            src="/images/Log-in.avif"
            alt="Login illustration"
            class="h-auto w-full drop-shadow-2xl"
          />
        </div>
        <h2 class="mb-4 text-3xl font-bold text-primary-700 dark:text-primary-400">
          Seamless Healthcare Management
        </h2>
        <p class="text-lg text-surface-600 dark:text-surface-400">
          Connect, organize, and manage your patients with NabdCare's intuitive platform.
        </p>
      </div>

      <div
        class="absolute -bottom-32 -left-20 h-96 w-96 rounded-full bg-primary-200/50 blur-3xl filter dark:bg-primary-700/20"
      ></div>
      <div
        class="absolute -right-20 -top-32 h-96 w-96 rounded-full bg-primary-300/50 blur-3xl filter dark:bg-primary-600/20"
      ></div>
    </div>

    <div class="flex w-full items-center justify-center px-6 sm:px-12 lg:w-1/2">
      <div class="w-full max-w-md animate-fade-in-up">
        <div class="mb-10 text-center">
          <div
            class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-xl bg-primary-100 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400"
          >
            <i class="pi pi-user text-2xl"></i>
          </div>
          <h1 class="mb-2 text-3xl font-bold text-surface-900 dark:text-surface-0">
            Welcome Back!
          </h1>
          <p class="text-surface-500 dark:text-surface-400">
            Enter your credentials to access your workspace.
          </p>
        </div>

        <form class="space-y-6" @submit.prevent="handleLogin" @keypress="handleKeyPress">
          <div class="w-full">
            <FloatLabel>
              <IconField>
                <InputIcon class="pi pi-envelope" />
                <InputText
                  :id="ids.email"
                  v-model.trim="email"
                  type="email"
                  class="w-full"
                  required
                  autocomplete="username"
                  :disabled="authStore.loading"
                />
              </IconField>
              <label :for="ids.email">Email Address</label>
            </FloatLabel>
          </div>

          <div class="w-full">
            <FloatLabel>
              <IconField>
                <InputIcon class="pi pi-lock z-10" />
                <Password
                  :id="ids.password"
                  v-model="password"
                  :toggle-mask="true"
                  :feedback="false"
                  class="w-full"
                  input-class="w-full pl-10"
                  required
                  autocomplete="current-password"
                  :disabled="authStore.loading"
                />
              </IconField>
              <label :for="ids.password">Password</label>
            </FloatLabel>

            <div class="mt-2 flex justify-end">
              <a
                href="#"
                class="text-sm font-medium text-primary-600 hover:text-primary-700 dark:text-primary-400"
              >
                Forgot password?
              </a>
            </div>
          </div>

          <div class="flex items-center">
            <Checkbox
              v-model="rememberMe"
              :input-id="ids.remember"
              :binary="true"
              class="mr-2"
              :disabled="authStore.loading"
            />
            <label
              :for="ids.remember"
              class="cursor-pointer select-none text-sm text-surface-600 transition-colors hover:text-surface-900 dark:text-surface-400 dark:hover:text-surface-200"
            >
              Keep me logged in
            </label>
          </div>

          <Button
            type="submit"
            label="Sign In"
            icon="pi pi-arrow-right"
            icon-pos="right"
            class="h-12 w-full text-lg font-semibold shadow-lg shadow-primary-500/30 transition-all hover:scale-[1.02] hover:shadow-primary-500/50"
            :loading="authStore.loading"
            :disabled="authStore.loading || !isFormValid"
            severity="primary"
            rounded
          />
        </form>

        <transition name="p-message" tag="div">
          <Message
            v-if="authStore.error"
            severity="error"
            :closable="true"
            class="mt-6 shadow-sm"
            icon="pi pi-exclamation-circle"
            @close="authStore.clearError()"
          >
            {{ authStore.error }}
          </Message>
        </transition>

        <div class="mt-10 text-center text-sm text-surface-500 dark:text-surface-400">
          <p>
            By signing in, you agree to our
            <a
              href="#"
              class="font-medium text-surface-900 underline decoration-surface-300 underline-offset-4 hover:text-primary-600 dark:text-surface-50"
            >
              Terms
            </a>
            and
            <a
              href="#"
              class="font-medium text-surface-900 underline decoration-surface-300 underline-offset-4 hover:text-primary-600 dark:text-surface-50"
            >
              Privacy Policy
            </a>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
  /* Keyframes for the floating animation on the illustration */
  @keyframes float {
    0% {
      transform: translateY(0px);
    }
    50% {
      transform: translateY(-15px);
    }
    100% {
      transform: translateY(0px);
    }
  }

  .animate-float {
    animation: float 6s ease-in-out infinite;
  }

  /* Entrance animation for the form */
  @keyframes fadeInUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  .animate-fade-in-up {
    animation: fadeInUp 0.6s ease-out forwards;
  }

  /* Deep Selectors to fix PrimeVue internal styling for icons inside Password component */
  :deep(.p-password) {
    width: 100%;
  }

  :deep(.p-password input) {
    width: 100%;
    padding-left: 2.5rem; /* Make room for the lock icon */
  }

  /* Ensure the icon inside the password field is positioned correctly */
  :deep(.p-icon-field .p-password) {
    width: 100%;
  }
</style>
