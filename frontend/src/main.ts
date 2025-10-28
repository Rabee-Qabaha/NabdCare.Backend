// src/main.ts
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
import { vPermission } from "./utils/permissions";

async function bootstrap() {
    const app = createApp(App);
    const pinia = createPinia();
    app.use(pinia);

    // ✅ Ensure auth is initialized before routing
    const authStore = useAuthStore();
    await authStore.initAuth();

    app.use(router);

    app.use(PrimeVue, {
        theme: {
            preset: Aura,
            options: { darkModeSelector: ".app-dark" },
        },
    });

    app.use(ToastService);
    app.use(ConfirmationService);

    // ✅ Global Permission Directive
    app.directive("permission", vPermission);

    await router.isReady();
    app.mount("#app");
}

bootstrap();