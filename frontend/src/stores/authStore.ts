import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { AuthService } from "@/service/AuthService";
import {
  getUserFromToken,
  isTokenExpired,
  type UserInfo,
} from "@/utils/jwtUtils";
import type { LoginRequestDto } from "@/types/backend";
import { UserRole } from "@/types/backend";

export const useAuthStore = defineStore("auth", () => {
  const currentUser = ref<UserInfo | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const isInitialized = ref(false);

  const isLoggedIn = computed(() => {
    const token = AuthService.getAccessToken();
    return !!token && !isTokenExpired(token);
  });

  const isSuperAdmin = computed(
    () => currentUser.value?.role === UserRole.SuperAdmin
  );
  const currentClinicId = computed(() => currentUser.value?.clinicId);
  const fullName = computed(
    () => currentUser.value?.fullName || currentUser.value?.email
  );

  // Normalized role as enum
  const role = computed<UserRole | null>(() => {
    if (!currentUser.value?.role) return null;
    const r = currentUser.value.role.toLowerCase();
    switch (r) {
      case "superadmin":
        return UserRole.SuperAdmin;
      case "clinicadmin":
        return UserRole.ClinicAdmin;
      case "doctor":
        return UserRole.Doctor;
      case "nurse":
        return UserRole.Nurse;
      case "receptionist":
        return UserRole.Receptionist;
      default:
        return null;
    }
  });

  const initAuth = (): void => {
    const token = AuthService.getAccessToken();
    if (token && !isTokenExpired(token)) {
      currentUser.value = getUserFromToken(token);
    } else {
      AuthService.clearTokens();
    }
    isInitialized.value = true;
  };

  const login = async (
    email: string,
    password: string,
    rememberMe = false
  ): Promise<UserInfo> => {
    loading.value = true;
    error.value = null;

    try {
      const tokens = await AuthService.login({
        email,
        password,
      } as LoginRequestDto);
      AuthService.storeTokens(tokens, rememberMe);
      currentUser.value = getUserFromToken(tokens.accessToken)!;
      return currentUser.value;
    } catch (err: any) {
      error.value = err.message || "Login failed";
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const logout = async () => {
    loading.value = true;
    try {
      await AuthService.logout();
    } finally {
      currentUser.value = null;
      AuthService.clearTokens();
      loading.value = false;
    }
  };

  const clearError = () => (error.value = null);

  return {
    currentUser,
    loading,
    error,
    isInitialized,
    isLoggedIn,
    isSuperAdmin,
    currentClinicId,
    fullName,
    role,
    initAuth,
    login,
    logout,
    clearError,
  };
});
