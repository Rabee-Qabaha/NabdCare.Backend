// src/main.ts
import App from '@/App.vue';
import { createPinia } from 'pinia';
import { createApp, watch } from 'vue';
import router from './router';

import Aura from '@primevue/themes/aura';
import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ToastService from 'primevue/toastservice';

import '@/assets/styles.scss';
import '@/assets/tailwind.css';

import { permissionDirective } from '@/directives/permission';
import { useAuthStore } from './stores/authStore';

// 🧠 Vue Query imports
import { queryClient } from '@/composables/query/queryClient';
import { VueQueryPlugin } from '@tanstack/vue-query';

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
