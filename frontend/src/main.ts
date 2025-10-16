import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import { createPinia } from "pinia";
import PrimeVue from "primevue/config";
import ToastService from "primevue/toastservice";
import Aura from "@primevue/themes/aura";
import ConfirmationService from "primevue/confirmationservice";
import { useAuthStore } from "./stores/authStore";

import "@/assets/styles.scss";
import "@/assets/tailwind.css";

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(PrimeVue, {
  theme: { preset: Aura, options: { darkModeSelector: ".app-dark" } },
});
app.use(ToastService);
app.use(ConfirmationService);

const authStore = useAuthStore();
authStore.restoreSession();

app.mount("#app");
