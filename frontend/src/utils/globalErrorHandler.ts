import { showToast } from '@/service/toastService';
import type { AxiosError } from 'axios';
// import { useToast } from "primevue/usetoast";

// const toast = useToast();

/**
 * Global error handler — Central control of API error display
 */
export async function globalErrorHandler(error: AxiosError) {
  const status = error.response?.status;
  const data = (error.response?.data ?? {}) as {
    message?: string;
    error?: string;
    detail?: string;
  };

  const message = data.message || data.error || data.detail || error.message || 'Unexpected error';

  // 🔒 1. Silent Errors — handled internally (no toast)
  const silentStatuses = [401, 403, 404];
  const silentEndpoints = ['/auth/refresh', '/auth/me'];

  if (
    (status && silentStatuses.includes(status)) ||
    silentEndpoints.some((ep) => error.config?.url?.includes(ep))
  ) {
    console.warn(`⚠️ Silent API Error: ${message}`);
    return;
  }

  // 🧯 2. Display user-facing errors
  showToast({
    severity: status && status >= 500 ? 'warn' : 'error',
    summary: 'Request Failed',
    detail: message,
    life: 4000,
  });

  // 🪵 3. Optionally log for analytics / Sentry
  if (import.meta.env.PROD) {
    // sendToSentry(error);
  }
}
