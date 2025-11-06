// src/main.ts
import { createApp, watch } from 'vue';
import App from '@/App.vue';
import router from './router';
import { createPinia } from 'pinia';

import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import ToastService from 'primevue/toastservice';
import ConfirmationService from 'primevue/confirmationservice';

import '@/assets/styles.scss';
import '@/assets/tailwind.css';

import { useAuthStore } from './stores/authStore';
import { permissionDirective } from './directives/permission'; // ✅ keep only this one

// 🧠 Vue Query imports
import { VueQueryPlugin } from '@tanstack/vue-query';
import { queryClient } from '@/composables/query/queryClient';

async function bootstrap() {
  const app = createApp(App);
  const pinia = createPinia();

  app.use(pinia);

  const authStore = useAuthStore();

  // ✅ Initialize user session
  await authStore.initAuth();

  // ✅ Clear Vue Query cache on logout
  watch(
    () => authStore.isLoggedIn,
    (loggedIn) => {
      if (!loggedIn) {
        console.log('🧹 Clearing Vue Query cache (user logged out)');
        queryClient.clear();
      }
    },
  );

  app.use(router);

  app.use(PrimeVue, {
    theme: {
      preset: Aura,
      options: { darkModeSelector: '.app-dark' },
    },
  });

  app.use(ToastService);
  app.use(ConfirmationService);

  // ✅ Register the permission directive once
  app.directive('permission', permissionDirective);

  // ✅ Vue Query setup
  app.use(VueQueryPlugin, { queryClient });

  await router.isReady();
  app.mount('#app');
}

bootstrap();
