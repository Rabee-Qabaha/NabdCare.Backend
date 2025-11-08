/**
 * Error Handler Composable
 * Location: src/composables/useErrorHandler.ts
 *
 * Use in components to handle errors.
 * Shows toasts, redirects on auth errors.
 *
 * Usage:
 * ```typescript
 * const { handleErrorAndNotify } = useErrorHandler();
 *
 * try {
 *   await createUser(data);
 * } catch (error) {
 *   handleErrorAndNotify(error);
 * }
 * ```
 *
 * User: Rabee-Qabaha
 * Updated: 2025-11-08 18:58:40
 */

import { handleError, getFieldErrors } from '@/utils/errorHandler';
import { useToastService } from '@/service/toastService';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import type { UIError } from '@/types/errors';

export function useErrorHandler() {
  const toast = useToastService();
  const router = useRouter();
  const authStore = useAuthStore();

  /**
   * Handle error and show appropriate UI response
   * - Show toast notification
   * - Redirect if needed (auth errors, access denied)
   * - Return processed error for further handling
   */
  const handleErrorAndNotify = async (error: any): Promise<UIError> => {
    const uiError = handleError(error);

    console.warn(`[${uiError.code}] ${uiError.message}`);

    // 🔒 SECURITY ERRORS - Force logout
    if (
      uiError.code === 'SECURITY_VIOLATION' ||
      uiError.code === 'TOKEN_REUSE_DETECTED'
    ) {
      console.error('🔒 Security violation - logging out');
      await authStore.logout();
      router.push('/auth/login');
      toast.error('Security violation detected. Please log in again.');
      return uiError;
    }

    // 🔓 AUTH ERRORS - Redirect to login
    if (uiError.isAuthError) {
      console.warn('🔓 Auth error - redirecting to login');
      await authStore.logout();
      router.push('/auth/login');
      toast.error(uiError.message);
      return uiError;
    }

    // 🚫 AUTHORIZATION ERRORS - Access denied
    if (
      uiError.code === 'FORBIDDEN' ||
      uiError.code === 'ACCESS_DENIED' ||
      uiError.code === 'INSUFFICIENT_PERMISSIONS'
    ) {
      console.warn('🚫 Authorization error');
      router.push('/auth/access');
      toast.error(uiError.message);
      return uiError;
    }

    // ✅ OTHER ERRORS - Just show toast
    toast.error(uiError.message);

    return uiError;
  };

  /**
   * Simple version - just show error in toast
   * Use this when you handle redirects manually
   */
  const showError = (error: any) => {
    const uiError = handleError(error);
    toast.error(uiError.message);
    return uiError;
  };

  /**
   * Get field errors from validation error
   * Use this in forms
   */
  const getValidationErrors = (error: any): Record<string, string> => {
    return getFieldErrors(error);
  };

  return {
    handleErrorAndNotify,
    showError,
    getValidationErrors,
  };
}