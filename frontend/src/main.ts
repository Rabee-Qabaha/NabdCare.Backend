// main.ts
import { createApp } from "vue";
import App from "@/App.vue";
import router from "./router";
import { createPinia } from "pinia";

import Aura from "@primevue/themes/aura";
import PrimeVue from "primevue/config";
import ConfirmationService from "primevue/confirmationservice";
import ToastService from "primevue/toastservice";

import "@/assets/styles.scss";
import "@/assets/tailwind.css";
import { useAuthStore } from "./stores/authStore";

async function bootstrap() {
  const app = createApp(App);
  const pinia = createPinia();

  app.use(pinia);

  const authStore = useAuthStore();
  authStore.initAuth();

  app.use(router);
  app.use(PrimeVue, {
    theme: {
      preset: Aura,
      options: {
        darkModeSelector: ".app-dark",
      },
    },
  });
  app.use(ToastService);
  app.use(ConfirmationService);

  await router.isReady();
  app.mount("#app");
}

bootstrap();
