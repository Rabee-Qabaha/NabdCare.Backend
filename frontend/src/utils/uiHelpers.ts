import type { PaymentMethod, VisitType } from '@/../../shared/types';

const formatterAlways2 = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'ILS',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const formatterNoDecimals = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'ILS',
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});

export function formatCurrency(value?: number): string {
  if (value == null) return '';
  const formatter = Number.isInteger(value) ? formatterNoDecimals : formatterAlways2;
  return formatter.format(value);
}

export function formatDate(value: string | number | Date | undefined) {
  if (!value) return '';
  let dateObj = value;
  if (typeof value === 'string' || typeof value === 'number') {
    dateObj = new Date(value);
  }
  if (dateObj instanceof Date && !isNaN(dateObj.getTime())) {
    const day = String(dateObj.getDate()).padStart(2, '0');
    const month = String(dateObj.getMonth() + 1).padStart(2, '0'); // Months are 0-based
    const year = dateObj.getFullYear();
    return `${day}/${month}/${year}`;
  }
  return '';
}

export function formatPhone(phone: string | undefined) {
  if (!phone) return '';
  const cleaned = ('' + phone).replace(/\D/g, '');
  const match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/);
  if (match) {
    return `${match[1]}-${match[2]}-${match[3]}`;
  }
  return phone;
}

export function truncate(text: string, length = 40): string {
  if (!text) return '';
  return text.length > length ? text.slice(0, length) + '…' : text;
}

export function getGenderIcon(gender: string) {
  switch (gender) {
    case 'Male':
      return 'pi pi-mars';
    case 'Female':
      return 'pi pi-venus';
    default:
      return 'pi pi-user';
  }
}

export function getGenderTagStyle(gender: string) {
  switch (gender) {
    case 'Male':
      return { backgroundColor: '#2196f3', color: '#fff' };
    case 'Female':
      return { backgroundColor: '#e91e63', color: '#fff' };
    default:
      return { backgroundColor: '#9e9e9e', color: '#fff' };
  }
}

// Default classes applied to all tags
const DEFAULT_TAG_CLASSES =
  'px-2 py-1 rounded text-xs font-semibold border-0 inline-flex items-center';

interface TagConfig {
  style: string;
  icon?: string;
}

// Define visit type colors
const visitTypeConfig: Record<VisitType, TagConfig> = {
  Initial: { style: 'bg-blue-300 text-blue-800' },
  Checkup: { style: 'bg-yellow-300 text-yellow-800' },
  Emergency: { style: 'bg-red-300 text-red-800' },
  Treatment: { style: 'bg-green-300 text-green-800' },
  Other: { style: 'bg-gray-300 text-gray-800' },
};

// Unified function returning classes
export function getVisitTypeTagClass(type: VisitType | string) {
  const style = visitTypeConfig[type as VisitType]?.style ?? 'bg-gray-300 text-gray-800';
  return `${DEFAULT_TAG_CLASSES} ${style}`;
}
interface PaymentStyleConfig {
  icon: string;
  style: string;
}

// Define payment method styles
const paymentConfig: Record<PaymentMethod, PaymentStyleConfig> = {
  Cash: { icon: 'pi pi-fw pi-money-bill', style: 'bg-green-300 text-green-900' },
  Cheque: { icon: 'pi pi-fw pi-receipt', style: 'bg-blue-300 text-blue-900' },
  Card: { icon: 'pi pi-fw pi-credit-card', style: 'bg-purple-300 text-purple-800' },
  BankTransfer: { icon: 'pi pi-fw pi-building-columns', style: 'bg-cyan-300 text-cyan-800' },
  Insurance: { icon: 'pi pi-fw pi-shield', style: 'bg-gray-300 text-gray-900' },
  Other: { icon: 'pi pi-fw pi-wallet', style: 'bg-yellow-300 text-yellow-900' },
};

// Unified function returning classes (including icon)
export function getPaymentRenderInfo(method: PaymentMethod | string) {
  const config = paymentConfig[method as PaymentMethod] ?? {
    icon: 'pi pi-fw pi-receipt',
    style: 'bg-gray-300 text-gray-800',
  };
  return {
    ...config,
    classes: `${DEFAULT_TAG_CLASSES} ${config.style}`,
  };
}
