import { defineStore } from "pinia";
import { ref, computed, watch } from "vue";
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
    // Fast fail: No user object
    if (!currentUser.value) {
      return false;
    }

    // Get token from secure storage
    const token = AuthService.getAccessToken();

    // No token = not logged in
    if (!token) {
      // ⚠️ Security: User exists but no token - clear user
      if (currentUser.value) {
        console.warn(
          "⚠️ User object exists but no token found - clearing state"
        );
        currentUser.value = null;
      }
      return false;
    }

    // Check token expiry
    if (isTokenExpired(token)) {
      console.warn("⚠️ Token expired - user will be logged out");
      return false;
    }

    // ✅ All checks passed
    return true;
  });

  // ✅ Enhanced: Watch for token expiry and auto-cleanup
  watch(isLoggedIn, (newValue, oldValue) => {
    // User just got logged out
    if (oldValue === true && newValue === false) {
      console.log("🔒 User logged out - cleaning up");
      currentUser.value = null;
      AuthService.clearTokens();
    }
  });

  const isSuperAdmin = computed(
    () => currentUser.value?.role === UserRole.SuperAdmin
  );

  const currentClinicId = computed(() => currentUser.value?.clinicId);

  const fullName = computed(
    () => currentUser.value?.fullName || currentUser.value?.email
  );

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

  /**
   * Initialize auth state on app load
   */
  const initAuth = (): void => {
    console.log("🔄 Initializing auth...");

    const token = AuthService.getAccessToken();

    if (!token) {
      console.log("ℹ️ No token found");
      currentUser.value = null;
      isInitialized.value = true;
      return;
    }

    if (isTokenExpired(token)) {
      console.log("⚠️ Token expired on init - clearing");
      AuthService.clearTokens();
      currentUser.value = null;
      isInitialized.value = true;
      return;
    }

    // Valid token found
    currentUser.value = getUserFromToken(token);
    console.log("✅ Auth initialized:", currentUser.value?.email);

    isInitialized.value = true;
  };

  /**
   * Login user
   */
  const login = async (
    email: string,
    password: string,
    _rememberMe = false
  ): Promise<UserInfo> => {
    loading.value = true;
    error.value = null;

    try {
      // Call backend
      const tokens = await AuthService.login({
        email,
        password,
      } as LoginRequestDto);

      // ✅ Extract and validate user from token
      const user = getUserFromToken(tokens.accessToken);
      if (!user) {
        throw new Error("Invalid token received from server");
      }

      // ✅ Update state (token is already stored by AuthService)
      currentUser.value = user;

      // ✅ DEBUG: Verify state
      console.log("✅ Login successful");
      console.log("   User:", currentUser.value.email);
      console.log("   Role:", currentUser.value.role);
      console.log("   isLoggedIn:", isLoggedIn.value);
      console.log("   Token exists:", !!AuthService.getAccessToken());

      return currentUser.value;
    } catch (err: any) {
      const errorMessage =
        err.response?.data?.error?.message || err.message || "Login failed";
      error.value = errorMessage;
      console.error("❌ Login failed:", errorMessage);
      throw new Error(errorMessage);
    } finally {
      loading.value = false;
    }
  };

  /**
   * Logout user
   */
  const logout = async () => {
    loading.value = true;
    try {
      await AuthService.logout();
      console.log("✅ Logout successful");
    } catch (err) {
      console.error("❌ Logout error:", err);
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
