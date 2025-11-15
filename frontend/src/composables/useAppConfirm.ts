// src/composables/useAppConfirm.ts
import { useConfirm } from 'primevue/useconfirm';

export function useAppConfirm() {
  const confirm = useConfirm();

  function show(options: {
    header: string;
    message: string;
    icon?: string;

    // Button text
    acceptLabel?: string;
    rejectLabel?: string;

    // Button styles (PrimeVue built-in)
    acceptSeverity?: 'primary' | 'success' | 'danger' | 'warning' | 'info' | 'help';
    rejectOutlined?: boolean;

    accept: () => void;
  }) {
    confirm.require({
      header: options.header,
      message: options.message,
      icon: options.icon ?? 'pi pi-exclamation-triangle',

      acceptLabel: options.acceptLabel ?? 'Confirm',
      rejectLabel: options.rejectLabel ?? 'Cancel',

      acceptProps: {
        severity: options.acceptSeverity ?? 'primary',
      },

      rejectProps: {
        outlined: options.rejectOutlined ?? true,
      },

      accept: options.accept,
    });
  }

  return { show };
}
