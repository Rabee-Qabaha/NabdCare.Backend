// src/stores/authStore.ts
import { defineStore } from 'pinia';
import { ref, computed, watch, nextTick } from 'vue';
import { AuthService } from '@/service/AuthService';
import { permissionsApi } from '@/api/modules/permissions';
import { getUserFromToken, isTokenExpired, type UserInfo } from '@/utils/jwtUtils';
import { PermissionRegistry } from '@/config/permissionsRegistry';
import type { LoginRequestDto } from '@/types/backend';

export const useAuthStore = defineStore('auth', () => {
  // 🔹 Reactive state
  const currentUser = ref<UserInfo | null>(null);
  const permissions = ref<string[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const isInitialized = ref(false);
  const isPermissionsLoaded = ref(false);

  // ===========================
  // 🧠 COMPUTED PROPERTIES (PURE - NO SIDE EFFECTS)
  // ===========================

  /**
   * ✅ Pure: Check if user is logged in
   * No side effects - just reads state
   */
  const isLoggedIn = computed(() => !!currentUser.value);

  /**
   * ✅ Check user role from JWT (primary source of truth)
   * Falls back to permission-based check
   */
  const userRole = computed(() => currentUser.value?.role || null);

  /**
   * ✅ Determine if user is SuperAdmin
   * PRIMARY: Check JWT role field
   * FALLBACK: Check system permissions
   */
  const isSuperAdmin = computed(() => {
    // 1️⃣ Primary: Check JWT role (most reliable)
    if (currentUser.value?.role === 'SuperAdmin') return true;

    // 2️⃣ Fallback: Check if permissions are loaded
    if (!isPermissionsLoaded.value) return false;

    // 3️⃣ Check multiple system permissions as secondary validation
    return permissions.value.some((p) =>
      [
        PermissionRegistry.System.manageSettings,
        PermissionRegistry.System.viewLogs,
        PermissionRegistry.System.manageRoles,
      ].includes(p),
    );
  });

  /**
   * ✅ Check if user has clinic context
   * Used to determine if user is clinic-based vs system admin
   */
  const hasClinicContext = computed(() => {
    if (!currentUser.value?.clinicId) return false;
    // Avoid "undefined" or null clinic IDs
    return currentUser.value.clinicId !== '00000000-0000-0000-0000-000000000000';
  });

  const currentClinicId = computed(() => currentUser.value?.clinicId);

  const fullName = computed(() => currentUser.value?.name || currentUser.value?.email || 'User');

  // ===========================
  // 🧹 WATCHERS (HANDLE SIDE EFFECTS)
  // ===========================

  /**
   * ✅ Watch token validity
   * If token expires, clear auth state
   */
  watch(
    () => AuthService.getAccessToken(),
    (token) => {
      if (!token || isTokenExpired(token)) {
        console.warn('⚠️ Token expired - clearing state');
        currentUser.value = null;
        permissions.value = [];
        isPermissionsLoaded.value = false;
      }
    },
    { immediate: true },
  );

  /**
   * ✅ Watch logout state
   * Clear session data when user logs out
   */
  watch(isLoggedIn, (newValue, oldValue) => {
    if (oldValue && !newValue) {
      console.log('🔒 Session ended - clearing auth state');
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = false;
      AuthService.clearTokens();
    }
  });

  // ===========================
  // 🔑 LOAD PERMISSIONS
  // ===========================

  /**
   * ✅ Load user permissions with retry logic
   * Resilient to temporary API failures
   */
  const loadPermissions = async (retries = 3): Promise<void> => {
    if (!currentUser.value) {
      permissions.value = [];
      isPermissionsLoaded.value = true;
      return;
    }

    for (let attempt = 0; attempt < retries; attempt++) {
      try {
        const data = await permissionsApi.getMine();
        console.log('🔍 Permissions API response:', data);

        permissions.value = data.permissions || [];
        console.log(`✅ Loaded ${permissions.value.length} permissions on attempt ${attempt + 1}`);
        isPermissionsLoaded.value = true;
        return;
      } catch (err) {
        console.error(`❌ Failed to load permissions (attempt ${attempt + 1}/${retries}):`, err);

        // Don't retry on 401/403 (auth errors)
        if ((err as any)?.response?.status === 401 || (err as any)?.response?.status === 403) {
          isPermissionsLoaded.value = true;
          permissions.value = [];
          throw err;
        }

        // Retry with exponential backoff
        if (attempt < retries - 1) {
          const delayMs = 1000 * Math.pow(2, attempt); // 1s, 2s, 4s
          console.log(`⏳ Retrying in ${delayMs}ms...`);
          await new Promise((resolve) => setTimeout(resolve, delayMs));
        }
      }
    }

    // ❌ All retries failed
    console.error('❌ Failed to load permissions after all retries');
    permissions.value = [];
    isPermissionsLoaded.value = true; // Mark as loaded even on failure
  };

  // ===========================
  // 🕒 WAIT FOR PERMISSIONS
  // ===========================

  /**
   * ✅ Wait for permissions to load with timeout
   * Prevents infinite waiting if API fails
   */
  const waitForPermissions = async (timeoutMs = 10000): Promise<void> => {
    if (isPermissionsLoaded.value) return;

    return new Promise<void>((resolve) => {
      let timeoutId: ReturnType<typeof setTimeout> | undefined;
      let resolved = false;

      const cleanup = () => {
        if (timeoutId !== undefined) clearTimeout(timeoutId);
        if (!resolved) {
          resolved = true;
          resolve();
        }
      };

      const stop = watch(isPermissionsLoaded, (loaded) => {
        if (loaded && !resolved) {
          resolved = true;
          stop();
          cleanup();
        }
      });

      // Timeout after specified duration
      timeoutId = setTimeout(() => {
        console.warn(`⚠️ Permission loading timeout after ${timeoutMs}ms - continuing anyway`);
        stop();
        cleanup();
      }, timeoutMs);
    });
  };

  // ===========================
  // 🔐 CHECK PERMISSION
  // ===========================

  /**
   * ✅ Check if user has specific permission
   * Returns false if permissions not loaded (safe)
   */
  const hasPermission = (permissionName: string): boolean => {
    if (!isPermissionsLoaded.value) return false;
    if (isSuperAdmin.value) return true;
    return permissions.value.includes(permissionName);
  };

  // ===========================
  // 🚀 INIT AUTH
  // ===========================

  /**
   * ✅ Initialize authentication on app startup
   * Restores user session from token if available
   */
  const initAuth = async (): Promise<void> => {
    console.log('🔄 Initializing auth...');
    isInitialized.value = false;
    isPermissionsLoaded.value = false;

    const token = AuthService.getAccessToken();

    if (!token) {
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = true;
      isInitialized.value = true;
      console.log('ℹ️ No token found - user is guest');
      return;
    }

    if (isTokenExpired(token)) {
      AuthService.clearTokens();
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = true;
      isInitialized.value = true;
      console.log('⚠️ Token expired - cleared');
      return;
    }

    // ✅ Restore user info from token
    const user = getUserFromToken(token);
    if (!user || !user.email) {
      console.error('❌ Invalid token payload');
      AuthService.clearTokens();
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = true;
      isInitialized.value = true;
      return;
    }

    currentUser.value = user;
    console.log('✅ User restored:', currentUser.value.email);

    // ✅ Load permissions asynchronously (don't block init)
    try {
      await loadPermissions();
    } catch (err) {
      console.error('⚠️ Permission loading failed during init:', err);
      // Continue anyway - user is still logged in
    }

    console.log('✅ Auth initialized:', currentUser.value.email, '| Role:', currentUser.value.role);
    isInitialized.value = true;
  };

  // ===========================
  // 🔓 LOGIN
  // ===========================

  /**
   * ✅ Login user and load permissions
   */
  const login = async (email: string, password: string, _rememberMe = false): Promise<UserInfo> => {
    loading.value = true;
    error.value = null;

    try {
      // ✅ AuthService returns { accessToken, user }
      const { accessToken, user } = await AuthService.login({
        email,
        password,
      } as LoginRequestDto);

      if (!accessToken || !user) throw new Error('Invalid login response');

      // ✅ Save decoded user info
      currentUser.value = user;

      // ✅ Load permissions
      await loadPermissions();

      // ✅ Wait one tick so reactivity settles before router push
      await nextTick();

      console.log('✅ Login successful:', currentUser.value.email);
      return currentUser.value;
    } catch (err: any) {
      const errorMessage = err.response?.data?.error?.message || err.message || 'Login failed';
      error.value = errorMessage;
      console.error('❌ Login failed:', errorMessage);
      currentUser.value = null;
      throw new Error(errorMessage);
    } finally {
      loading.value = false;
    }
  };

  // ===========================
  // 🚪 LOGOUT
  // ===========================

  /**
   * ✅ Logout user and clear all state
   */
  const logout = async () => {
    loading.value = true;
    try {
      await AuthService.logout();
    } finally {
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = false;
      AuthService.clearTokens();
      loading.value = false;
    }
  };

  // ===========================
  // 🧹 MISC
  // ===========================

  const clearError = () => (error.value = null);

  // ===========================
  // 📤 EXPORT STORE
  // ===========================

  return {
    // State
    currentUser,
    permissions,
    loading,
    error,
    isInitialized,
    isLoggedIn,
    isSuperAdmin,
    userRole,
    hasClinicContext,
    currentClinicId,
    fullName,
    isPermissionsLoaded,

    // Actions
    initAuth,
    login,
    logout,
    loadPermissions,
    waitForPermissions,
    hasPermission,
    clearError,
  };
});
