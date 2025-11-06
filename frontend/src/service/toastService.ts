// src/service/toastService.ts
import { useToast } from 'primevue/usetoast';

let toast: ReturnType<typeof useToast> | null = null;

export function setToastInstance(instance: ReturnType<typeof useToast>) {
  toast = instance;
}

export function showToast(opts: {
  severity: string;
  summary: string;
  detail?: string;
  life?: number;
}) {
  toast?.add(opts);
}
