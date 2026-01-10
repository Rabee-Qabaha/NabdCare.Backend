// src/utils/uiHelpers.ts
import type { ClinicSettingsDto } from '@/types/backend';

const SYSTEM_LOCALE = 'en-US';
const SYSTEM_CURRENCY = 'USD'; // Fallback only

export function formatCurrency(
  value: number | undefined | null,
  currency: string = SYSTEM_CURRENCY,
  locale: string = SYSTEM_LOCALE,
): string {
  if (value === undefined || value === null) return '-';

  try {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  } catch (error) {
    return `${value.toFixed(2)} ${currency}`;
  }
}

export function formatDate(
  date: string | Date | undefined | null,
  locale: string = SYSTEM_LOCALE,
  options?: Intl.DateTimeFormatOptions,
): string {
  if (!date) return '-';
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  if (isNaN(dateObj.getTime())) return '-';

  const defaultOptions: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    ...options,
  };

  try {
    return new Intl.DateTimeFormat(locale, defaultOptions).format(dateObj);
  } catch (error) {
    return dateObj.toLocaleDateString(locale);
  }
}

// ✅ UPDATED: Allow null/undefined for value
export function formatClinicCurrency(
  value: number | undefined | null,
  settings?: ClinicSettingsDto | null,
): string {
  return formatCurrency(
    value,
    settings?.currency || SYSTEM_CURRENCY,
    settings?.locale || SYSTEM_LOCALE,
  );
}

// ✅ UPDATED: Allow null/undefined for date
export function formatClinicDate(
  date: string | Date | undefined | null,
  settings?: ClinicSettingsDto | null,
): string {
  return formatDate(date, settings?.locale || SYSTEM_LOCALE);
}

export function formatDateTime(
  date: string | Date | undefined | null,
  locale: string = SYSTEM_LOCALE,
): string {
  return formatDate(date, locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
