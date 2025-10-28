// frontend/src/stores/authStore.ts
import { defineStore } from "pinia";
import { ref, computed, watch } from "vue";
import { AuthService } from "@/service/AuthService";
import { apiService } from "@/service/apiService";
import {
  getUserFromToken,
  isTokenExpired,
  type UserInfo,
} from "@/utils/jwtUtils";
import type { LoginRequestDto, PermissionResponseDto } from "@/types/backend";
import { UserRole } from "@/types/backend/user-role";

export const useAuthStore = defineStore("auth", () => {
  const currentUser = ref<UserInfo | null>(null);
  const permissions = ref<string[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const isInitialized = ref(false);

  const isLoggedIn = computed(() => {
    if (!currentUser.value) return false;
    const token = AuthService.getAccessToken();
    if (!token) {
      if (currentUser.value) {
        console.warn(
          "⚠️ User object exists but no token found - clearing state"
        );
        currentUser.value = null;
      }
      return false;
    }
    if (isTokenExpired(token)) {
      console.warn("⚠️ Token expired - user will be logged out");
      return false;
    }
    return true;
  });

  watch(isLoggedIn, (newValue, oldValue) => {
    if (oldValue === true && newValue === false) {
      console.log("🔒 User logged out - cleaning up");
      currentUser.value = null;
      permissions.value = []; // ✅ ADD THIS
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
    if (!currentUser.value?.role) {
      console.warn("⚠️ No role found in user:", currentUser.value);
      return null;
    }

    const r = currentUser.value.role.toLowerCase().trim();

    console.log("🔍 Normalizing role:", currentUser.value.role, "→", r);

    switch (r) {
      case "superadmin":
      case "super admin":
        return UserRole.SuperAdmin;
      case "clinicadmin":
      case "clinic admin":
        return UserRole.ClinicAdmin;
      case "doctor":
        return UserRole.Doctor;
      case "nurse":
        return UserRole.Nurse;
      case "receptionist":
        return UserRole.Receptionist;
      default:
        console.warn("⚠️ Unknown role:", currentUser.value.role);
        return null;
    }
  });

  // ✅ ADD THIS FUNCTION
  /**
   * Load user's effective permissions from backend
   */
  const loadPermissions = async (): Promise<void> => {
    if (!currentUser.value) {
      permissions.value = [];
      return;
    }

    try {
      const userPermissions =
        await apiService.get<PermissionResponseDto[]>("/permissions/me");
      permissions.value = userPermissions.map((p) => p.name);
      console.log("✅ Loaded permissions:", permissions.value.length);
    } catch (err) {
      console.error("❌ Failed to load permissions:", err);
      permissions.value = [];
    }
  };

  // ✅ ADD THIS FUNCTION
  /**
   * Check if user has a specific permission
   */
  const hasPermission = (permissionName: string): boolean => {
    // SuperAdmin has ALL permissions
    if (isSuperAdmin.value) return true;
    return permissions.value.includes(permissionName);
  };

  /**
   * Initialize auth state on app load
   */
  const initAuth = async (): Promise<void> => {
    // ✅ MAKE IT async
    console.log("🔄 Initializing auth...");

    const token = AuthService.getAccessToken();

    if (!token) {
      console.log("ℹ️ No token found");
      currentUser.value = null;
      permissions.value = []; // ✅ ADD THIS
      isInitialized.value = true;
      return;
    }

    if (isTokenExpired(token)) {
      console.log("⚠️ Token expired on init - clearing");
      AuthService.clearTokens();
      currentUser.value = null;
      permissions.value = []; // ✅ ADD THIS
      isInitialized.value = true;
      return;
    }

    currentUser.value = getUserFromToken(token);

    // ✅ ADD THIS
    if (currentUser.value) {
      await loadPermissions();
    }

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
      const tokens = await AuthService.login({
        email,
        password,
      } as LoginRequestDto);

      const user = getUserFromToken(tokens.accessToken);
      if (!user) {
        throw new Error("Invalid token received from server");
      }

      currentUser.value = user;

      // ✅ ADD THIS
      await loadPermissions();

      console.log("✅ Login successful");
      console.log("   User:", currentUser.value.email);
      console.log("   Role:", currentUser.value.role);
      console.log("   Permissions:", permissions.value.length); // ✅ ADD THIS

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
      permissions.value = []; // ✅ ADD THIS
      AuthService.clearTokens();
      loading.value = false;
    }
  };

  const clearError = () => (error.value = null);

  return {
    currentUser,
    permissions, // ✅ ADD THIS
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
    loadPermissions, // ✅ ADD THIS
    hasPermission, // ✅ ADD THIS
    clearError,
  };
});
