/**
 * Toast Service
 * Location: src/service/toastService.ts
 *
 * Global toast management for the application.
 * Provides both functional and composable interfaces.
 *
 * User: Rabee-Qabaha
 * Updated: 2025-11-08 19:08:58
 */

import { useToast as usePrimeToast } from 'primevue/usetoast';

let toastInstance: ReturnType<typeof usePrimeToast> | null = null;

/**
 * Set the global toast instance (called in App.vue)
 */
export function setToastInstance(instance: ReturnType<typeof usePrimeToast>) {
  toastInstance = instance;
}

/**
 * Show toast notification (functional approach)
 * Used in interceptors and non-component code
 */
export function showToast(opts: {
  severity: 'success' | 'info' | 'warn' | 'error';
  summary: string;
  detail?: string;
  life?: number;
}) {
  toastInstance?.add({
    severity: opts.severity,
    summary: opts.summary,
    detail: opts.detail,
    life: opts.life || 3000,
  });
}

/**
 * Composable approach (for components)
 * Usage in components:
 * ```typescript
 * const toast = useToastService();
 * toast.success('Done!');
 * toast.error('Error!');
 * ```
 */
export function useToastService() {
  const toast = usePrimeToast();

  return {
    /**
     * Show success toast
     */
    success: (message: string, summary = 'Success', life = 3000) => {
      toast.add({
        severity: 'success',
        summary,
        detail: message,
        life,
      });
    },

    /**
     * Show error toast
     */
    error: (message: string, summary = 'Error', life = 4000) => {
      toast.add({
        severity: 'error',
        summary,
        detail: message,
        life,
      });
    },

    /**
     * Show warning toast
     */
    warning: (message: string, summary = 'Warning', life = 3000) => {
      toast.add({
        severity: 'warn',
        summary,
        detail: message,
        life,
      });
    },

    /**
     * Show info toast
     */
    info: (message: string, summary = 'Info', life = 3000) => {
      toast.add({
        severity: 'info',
        summary,
        detail: message,
        life,
      });
    },

    /**
     * Generic toast
     */
    add: (opts: {
      severity: 'success' | 'info' | 'warn' | 'error';
      summary: string;
      detail?: string;
      life?: number;
    }) => {
      toast.add({
        severity: opts.severity,
        summary: opts.summary,
        detail: opts.detail,
        life: opts.life || 3000,
      });
    },
  };
}