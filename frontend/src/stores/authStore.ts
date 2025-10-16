// src/stores/authStore.ts
import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { useToast } from "primevue/usetoast";
import { login } from "@/types/sdk.gen";
import { AuthService } from "@/service/AuthService";
import { jwtDecode } from "@/utils/jwtDecoder";
import { apiClient } from "@/api/apiClientInstance";
import { UserRole } from "@/types/sdk";

interface JwtPayload {
  email: string;
  role: string;
  clinicId?: string;
}

export const useAuthStore = defineStore("authStore", () => {
  const toast = useToast();

  // -------------------------
  // State
  // -------------------------
  const accessToken = ref<string | null>(AuthService.getAccessToken());
  const refreshToken = ref<string | null>(AuthService.getRefreshToken());
  const user = ref<JwtPayload | null>(
    accessToken.value ? jwtDecode<JwtPayload>(accessToken.value) : null
  );
  const loading = ref(false);
  const isInitialized = ref(false);

  // -------------------------
  // Getters
  // -------------------------
  const isLoggedIn = computed(() => !!accessToken.value);
  const isSuperAdmin = computed(() => user.value?.role === UserRole.SuperAdmin);
  const isClinicAdmin = computed(
    () => user.value?.role === UserRole.ClinicAdmin
  );

  // -------------------------
  // Actions
  // -------------------------
  const setUserFromToken = (token: string) => {
    const decoded = jwtDecode<JwtPayload>(token);
    if (decoded) user.value = decoded;
  };

  const loginUser = async (email: string, password: string) => {
    loading.value = true;
    try {
      const response = await login({
        client: apiClient,
        body: { email, password },
      });

      // Explicitly type response data
      const data = response.data as {
        accessToken: string;
        refreshToken: string;
      };

      AuthService.saveTokens(data.accessToken, data.refreshToken);

      accessToken.value = data.accessToken;
      refreshToken.value = data.refreshToken;
      setUserFromToken(data.accessToken);

      toast.add({
        severity: "success",
        summary: "Welcome!",
        detail: `Hello, ${email}`,
        life: 3000,
      });

      return data;
    } catch (err: any) {
      toast.add({
        severity: "error",
        summary: "Login Failed",
        detail: err?.message || "Invalid email or password",
        life: 3000,
      });
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const logoutUser = () => {
    AuthService.clearTokens();
    accessToken.value = null;
    refreshToken.value = null;
    user.value = null;

    toast.add({
      severity: "info",
      summary: "Logged out",
      detail: "You have successfully logged out.",
      life: 3000,
    });
  };

  const restoreSession = () => {
    const token = AuthService.getAccessToken();
    if (token) {
      accessToken.value = token;
      refreshToken.value = AuthService.getRefreshToken();
      setUserFromToken(token);
    }
    isInitialized.value = true;
  };

  return {
    // state
    accessToken,
    refreshToken,
    user,
    loading,
    isInitialized,

    // getters
    isLoggedIn,
    isSuperAdmin,
    isClinicAdmin,

    // actions
    setUserFromToken,
    loginUser,
    logoutUser,
    restoreSession,
  };
});
