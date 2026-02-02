<template>
  <div
    v-if="hasOverdueInvoices"
    class="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4 rounded-r shadow-sm flex items-start justify-between"
  >
    <div class="flex items-start gap-3">
      <i class="pi pi-exclamation-circle text-red-600 dark:text-red-400 text-xl mt-0.5"></i>
      <div>
        <h3 class="font-bold text-red-900 dark:text-red-100 text-sm">
          Outstanding Invoices Detected
        </h3>
        <p class="text-sm text-red-700 dark:text-red-300 mt-1">
          This clinic has unpaid invoices. Services may be at risk of suspension.
        </p>
      </div>
    </div>
    <button
      class="text-xs font-bold bg-white dark:bg-red-900 text-red-600 dark:text-red-200 px-3 py-1.5 rounded border border-red-100 dark:border-red-800 shadow-sm hover:bg-red-50 transition-colors"
    >
      View Billing
    </button>
  </div>
</template>

<script setup lang="ts">
  defineProps<{
    hasOverdueInvoices: boolean;
  }>();
</script>
