// src/api/errorInterceptor.ts
/**
 * Error Interceptor
 * Location: src/api/errorInterceptor.ts
 *
 * Setup axios to intercept and process errors.
 * Logs errors for debugging.
 *
 * User: Rabee-Qabaha
 * Updated: 2025-11-08 18:58:40
 */

import { handleError } from '@/utils/errorHandler';
import type { AxiosInstance } from 'axios';

/**
 * Setup error interceptor on axios instance
 * Processes all responses with errors
 */
export function setupErrorInterceptor(apiClient: AxiosInstance) {
  apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
      // ✅ Process error
      const uiError = handleError(error);

      // ✅ Log for debugging
      console.error(`🚨 API Error [${uiError.code}]`, {
        message: uiError.message,
        status: uiError.httpStatus,
        url: error.config?.url,
        code: uiError.code,
      });

      // ✅ Don't show toast for auth refresh endpoints (handled by route guard)
      const silentEndpoints = ['/auth/refresh', '/auth/me'];
      const isSilent = silentEndpoints.some((ep) => error.config?.url?.includes(ep));

      if (isSilent) {
        console.debug('🤐 Silent error (will be handled by route guard)');
      }

      // ✅ Reject with processed error
      return Promise.reject(uiError);
    },
  );
}
