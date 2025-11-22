// src/composables/errorHandling/useErrorHandler.ts
/**
 * Full Error Handler Composable
 * Location: src/composables/errorHandling/useErrorHandler.ts
 *
 * Handles:
 * - API errors
 * - Toasts
 * - Field validation parsing
 * - Auth redirect logic
 */

import { useToastService } from '@/composables/useToastService';
import { useAuthStore } from '@/stores/authStore';
import type { UIError } from '@/types/errors';
import { getFieldErrors, handleError as processError } from '@/utils/errorHandler';
import { useRouter } from 'vue-router';

export function useErrorHandler() {
  const router = useRouter();
  const authStore = useAuthStore();
  const toast = useToastService();

  /**
   * Main handler used everywhere (mutations, queries, forms)
   */
  const handleErrorAndNotify = async (error: any): Promise<UIError> => {
    const uiError = processError(error);

    console.warn(`[${uiError.code}] ${uiError.message}`);

    // 🔐 TOKEN / SECURITY VIOLATIONS
    if (uiError.code === 'SECURITY_VIOLATION' || uiError.code === 'TOKEN_REUSE_DETECTED') {
      await authStore.logout();
      router.push('/auth/login');
      toast.error('Security violation detected. Please log in again.');
      return uiError;
    }

    // 🔓 AUTH ERRORS
    if (uiError.isAuthError) {
      await authStore.logout();
      router.push('/auth/login');
      toast.error(uiError.message);
      return uiError;
    }

    // 🚫 AUTHORIZATION ERRORS
    if (
      uiError.code === 'FORBIDDEN' ||
      uiError.code === 'ACCESS_DENIED' ||
      uiError.code === 'INSUFFICIENT_PERMISSIONS'
    ) {
      router.push('/auth/access');
      toast.error(uiError.message);
      return uiError;
    }

    // ⭐ DEFAULT CASE
    toast.error(uiError.message);
    return uiError;
  };

  /**
   * Basic toast-only error handler (no redirects)
   */
  const showError = (error: any): UIError => {
    const uiError = processError(error);
    toast.error(uiError.message);
    return uiError;
  };

  /**
   * For form-level validation
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
