<script setup lang="ts">
  import BaseCard from '@/components/shared/BaseCard.vue';
  import { useInfiniteInvoicesPaged } from '@/composables/query/invoices/useInvoices';
  import type { SubscriptionResponseDto } from '@/types/backend';
  import { InvoiceStatus, SubscriptionStatus } from '@/types/backend';
  import Button from 'primevue/button';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  const props = defineProps<{
    subscription: SubscriptionResponseDto;
    clinicId: string;
    planName?: string | null;
  }>();

  // 🛡️ Safety Check: Fetch any overdue invoices (even if latest is paid)
  // We keep this query local to the header as it drives the alert
  const { data: overdueInvoicesData } = useInfiniteInvoicesPaged({
    clinicId: computed(() => props.clinicId),
    status: InvoiceStatus.Overdue,
    limit: 1,
  });

  // -- Computed Properties --

  const daysRemaining = computed(() => {
    if (!props.subscription.endDate) return 0;
    const end = new Date(props.subscription.endDate).getTime();
    const now = new Date().getTime();
    const diffInMs = end - now;
    const diffInDays = diffInMs / (1000 * 60 * 60 * 24);
    return Math.max(0, Math.ceil(diffInDays));
  });

  const hasOverdueInvoices = computed(() => {
    const pages = overdueInvoicesData.value?.pages;
    return pages?.[0]?.items?.length ? pages[0].items.length > 0 : false;
  });

  const isPaymentActionRequired = computed(() => {
    const sub = props.subscription;

    // 1. Check Subscription Status (Primary)
    if (sub.status === SubscriptionStatus.PastDue || sub.status === SubscriptionStatus.Suspended) {
      return true;
    }

    // 2. Check for ANY Overdue Invoices (Safety Net)
    if (hasOverdueInvoices.value) {
      return true;
    }

    // 3. Check Latest Invoice Status (Backend Provided)
    const status = sub.latestInvoiceStatus;
    if (status) {
      return (
        status === InvoiceStatus.Overdue ||
        status === InvoiceStatus.PartiallyPaid ||
        status === InvoiceStatus.Issued
      );
    }

    return false;
  });

  const paymentStatusLabel = computed(() => {
    const sub = props.subscription;
    if (sub.status === SubscriptionStatus.Suspended) return 'Subscription Suspended';
    if (sub.status === SubscriptionStatus.PastDue) return 'Payment Past Due';

    if (hasOverdueInvoices.value) return 'Unpaid Invoices';

    const status = sub.latestInvoiceStatus;
    if (!status) return 'Action Required';

    if (status === InvoiceStatus.Overdue) return 'Invoice Overdue';
    if (status === InvoiceStatus.PartiallyPaid) return 'Balance Due';
    if (status === InvoiceStatus.Issued) return 'Payment Required';

    return 'Action Required';
  });
</script>

<template>
  <BaseCard class="p-5 flex flex-col md:flex-row justify-between items-center gap-4 mb-6">
    <div class="flex items-center gap-4 w-full md:w-auto">
      <div
        class="w-12 h-12 rounded-lg bg-primary-100 dark:bg-primary-400/10 text-primary-600 dark:text-primary-400 flex items-center justify-center shadow-sm"
      >
        <i class="pi pi-star-fill text-xl"></i>
      </div>

      <div>
        <div class="flex items-center gap-3">
          <h2 class="text-xl font-bold text-surface-900 dark:text-surface-0">
            {{ planName || subscription.planId }}
          </h2>
          <Tag
            value="ACTIVE"
            severity="success"
            class="!px-2 !py-0.5 !text-[10px] !font-bold"
            rounded
          />
        </div>

        <div class="flex items-center gap-2 text-muted-color text-sm font-medium mt-1">
          <i class="pi pi-calendar"></i>
          <span>
            Renewal in
            <span class="text-surface-900 dark:text-surface-0 font-bold">
              {{ daysRemaining }} Days
            </span>
          </span>
        </div>
      </div>
    </div>

    <div class="flex flex-col sm:flex-row items-center gap-3 w-full md:w-auto">
      <div
        v-if="isPaymentActionRequired"
        class="relative flex items-center gap-3 pl-3 pr-5 py-1.5 bg-gradient-to-r from-red-50 to-orange-50/50 dark:from-red-900/20 dark:to-orange-900/10 rounded-lg border border-red-200/60 dark:border-red-500/20 backdrop-blur-sm shadow-sm transition-all hover:shadow-md hover:border-red-300 dark:hover:border-red-500/30 group cursor-default"
      >
        <!-- Status Indicator Dot -->
        <span class="relative flex h-2.5 w-2.5 shrink-0">
          <span
            class="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"
          ></span>
          <span class="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500"></span>
        </span>

        <div class="flex flex-col">
          <span class="text-[9px] uppercase font-bold text-red-500/70 leading-tight tracking-wider">
            Action Required
          </span>
          <span
            class="text-xs font-bold text-red-700 dark:text-red-400 leading-tight group-hover:text-red-800 dark:group-hover:text-red-300 transition-colors"
          >
            {{ paymentStatusLabel }}
          </span>
        </div>
      </div>

      <Button
        label="Manage Add-ons"
        icon="pi pi-sliders-h"
        class="!bg-primary-500 !border-primary-500 hover:!bg-primary-600 !rounded-lg !font-bold w-full sm:w-auto"
      />
      <Button
        label="Renew Subscription"
        icon="pi pi-refresh"
        class="!bg-green-500 !border-green-500 hover:!bg-green-600 !rounded-lg !font-bold w-full sm:w-auto"
      />
      <Button
        label="Cancel Subscription"
        icon="pi pi-times"
        class="!bg-red-500 !border-red-500 hover:!bg-red-600 !rounded-lg !font-bold w-full sm:w-auto"
      />
    </div>
  </BaseCard>
</template>
