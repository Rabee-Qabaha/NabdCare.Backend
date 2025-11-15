// src/stores/authStore.ts
import { authApi } from '@/api/modules/auth';
import { permissionsApi } from '@/api/modules/permissions';
import { PermissionRegistry } from '@/config/permissionsRegistry';
import { usersApi } from "@/api/modules/users";

import type { LoginRequestDto, UserResponseDto } from '@/types/backend';
import { defineStore } from 'pinia';
import { ref, computed, nextTick } from 'vue';

import { tokenManager } from '@/utils/tokenManager';
import { isTokenExpired } from '@/utils/jwtUtils';

export const useAuthStore = defineStore('auth', () => {
  // ===========================
  // STATE
  // ===========================
  const currentUser = ref<UserResponseDto | null>(null);
  const permissions = ref<string[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const isInitialized = ref(false);
  const isPermissionsLoaded = ref(false);

  // ===========================
  // COMPUTED
  // ===========================
  const isLoggedIn = computed(() => !!currentUser.value);

  const userRole = computed(() => currentUser.value?.roleName ?? null);

  const isSuperAdmin = computed(() => {
    if (currentUser.value?.roleName === 'SuperAdmin') return true;
    if (!isPermissionsLoaded.value) return false;

    return permissions.value.some((p) =>
      [
        PermissionRegistry.System.manageSettings,
        PermissionRegistry.System.viewLogs,
        PermissionRegistry.System.manageRoles,
      ].includes(p),
    );
  });

  const hasClinicContext = computed(() => {
    const cid = currentUser.value?.clinicId;
    return !!cid && cid !== '00000000-0000-0000-0000-000000000000';
  });

  const currentClinicId = computed(() => currentUser.value?.clinicId ?? null);

  const fullName = computed(
    () => currentUser.value?.fullName || currentUser.value?.email || 'User',
  );

  // ===========================
  // LOAD USER FROM BACKEND
  // ===========================
  const loadCurrentUser = async () => {
    try {
      const response = await usersApi.getMe()
      currentUser.value = response.data;
    } catch (err) {
      console.error('❌ Failed to fetch /users/me:', err);
      currentUser.value = null;
      tokenManager.clearTokens();
    }
  };

  // ===========================
  // LOAD PERMISSIONS
  // ===========================
  const loadPermissions = async () => {
    if (!currentUser.value) {
      permissions.value = [];
      isPermissionsLoaded.value = true;
      return;
    }

    try {
      const result = await permissionsApi.getMine();
      permissions.value = result.permissions || [];
    } catch (err) {
      console.error('❌ Failed to load permissions:', err);
      permissions.value = [];
    }

    isPermissionsLoaded.value = true;
  };

  // ===========================
  // INIT AUTH (APP STARTUP)
  // ===========================
  const initAuth = async () => {
    console.log('🔄 Initializing auth...');

    isInitialized.value = false;
    isPermissionsLoaded.value = false;

    const token = tokenManager.getAccessToken();

    if (!token) {
      console.log('ℹ️ No access token — guest mode');
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = true;
      isInitialized.value = true;
      return;
    }

    if (isTokenExpired(token)) {
      console.log('⚠️ Token expired — clearing');
      tokenManager.clearTokens();
      currentUser.value = null;
      permissions.value = [];
      isPermissionsLoaded.value = true;
      isInitialized.value = true;
      return;
    }

    // ✅ Fetch user from backend
    await loadCurrentUser();

    // If user failed to load
    if (!currentUser.value) {
      isInitialized.value = true;
      isPermissionsLoaded.value = true;
      return;
    }

    // Load permissions
    await loadPermissions();

    isInitialized.value = true;
    console.log('✅ Auth initialized:', currentUser.value.email);
  };

  // ===========================
  // LOGIN
  // ===========================
  const login = async (email: string, password: string) => {
    loading.value = true;
    error.value = null;

    try {
      const response = await authApi.login({
        email,
        password,
      } as LoginRequestDto);

      const accessToken = response.accessToken;
      if (!accessToken) throw new Error('Missing accessToken');

      // Save token
      tokenManager.setAccessToken(accessToken, true);

      // Fetch user profile
      await loadCurrentUser();

      if (!currentUser.value) throw new Error('Failed to load user info');

      await loadPermissions();
      await nextTick();

      return currentUser.value;
    } catch (err: any) {
      const message =
        err?.response?.data?.error?.message ||
        err?.message ||
        'Login failed';

      error.value = message;
      currentUser.value = null;
      tokenManager.clearTokens();

      throw new Error(message);
    } finally {
      loading.value = false;
    }
  };

  // ===========================
  // LOGOUT
  // ===========================
  const logout = async () => {
    loading.value = true;

    try {
      await authApi.logout();
    } catch {
      console.warn('⚠️ logout failed but ignoring');
    }

    currentUser.value = null;
    permissions.value = [];
    tokenManager.clearTokens();
    isPermissionsLoaded.value = true;
    loading.value = false;
  };

  const hasPermission = (permission: string) =>
    isSuperAdmin.value || permissions.value.includes(permission);

  const clearError = () => (error.value = null);

  // ===========================
  // EXPORT
  // ===========================
  return {
    // State
    currentUser,
    permissions,
    loading,
    error,
    isInitialized,
    isPermissionsLoaded,

    // Computed
    isLoggedIn,
    isSuperAdmin,
    userRole,
    fullName,
    currentClinicId,
    hasClinicContext,

    // Actions
    initAuth,
    login,
    logout,
    loadCurrentUser,
    loadPermissions,
    hasPermission,
    clearError,
  };
});