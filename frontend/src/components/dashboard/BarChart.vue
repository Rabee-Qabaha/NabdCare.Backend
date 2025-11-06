<script setup lang="ts">
  import { ref, watch, onMounted } from 'vue';
  import { useLayout } from '@/layout/composables/layout';
  import { formatCurrency } from '@/utils/uiHelpers';

  defineProps({
    title: { type: String, default: 'Bar Chart' },
    data: { type: Object, required: true },
  });

  const { getPrimary, getSurface, isDarkTheme } = useLayout();
  const chartOptions = ref({});

  // --- Constants for styles ---
  function setChartOptions() {
    const documentStyle = getComputedStyle(document.documentElement);
    const borderColor = documentStyle.getPropertyValue('--surface-border');
    const textColor = documentStyle.getPropertyValue('--text-color');
    const textMutedColor = documentStyle.getPropertyValue('--text-color-secondary');

    return {
      maintainAspectRatio: false,
      aspectRatio: 0.8,
      plugins: {
        legend: { labels: { color: textColor } },
        tooltip: {
          mode: 'index',
          intersect: false,
          callbacks: {
            label: (context: any) => {
              const value = context.raw || 0;
              return `${context.dataset.label}: ${formatCurrency(value)}`;
            },
          },
        },
      },
      scales: {
        x: {
          stacked: true,
          ticks: { color: textMutedColor },
          grid: { color: 'transparent', borderColor: 'transparent' },
        },
        y: {
          stacked: true,
          ticks: { color: textMutedColor },
          grid: { color: borderColor, borderColor: 'transparent', drawTicks: false },
        },
      },
    };
  }

  watch([getPrimary, getSurface, isDarkTheme], () => {
    chartOptions.value = setChartOptions();
  });

  onMounted(() => {
    chartOptions.value = setChartOptions();
  });
</script>

<template>
  <div class="card flex h-full flex-col">
    <div class="mb-4 text-xl font-semibold">{{ title }}</div>
    <div class="flex-1">
      <Chart type="bar" :data="data" :options="chartOptions" class="h-full w-full" />
    </div>
  </div>
</template>
