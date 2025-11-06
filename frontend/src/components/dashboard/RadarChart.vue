<script setup lang="ts">
  import { ref, watch, onMounted } from 'vue';
  import { useLayout } from '@/layout/composables/layout';
  import { formatCurrency } from '@/utils/uiHelpers';

  defineProps({
    title: { type: String, default: 'Polar Area' },
    data: { type: Object, required: true },
  });

  const { getPrimary, getSurface, isDarkTheme } = useLayout();
  const chartOptions = ref({});

  function setChartOptions() {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--text-color');
    const textMutedColor = documentStyle.getPropertyValue('--text-color-secondary');
    const borderColor = documentStyle.getPropertyValue('--surface-border');

    return {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: textColor },
          position: 'bottom',
        },
        tooltip: {
          backgroundColor: borderColor,
          titleColor: textColor,
          bodyColor: textMutedColor,
          callbacks: {
            label: function (context: any) {
              const value = context.raw;
              if (context.dataset.currency) {
                return ` ${formatCurrency(value)}`;
              }
              return ` ${value}`;
            },
          },
        },
      },
      scales: {
        r: {
          grid: { color: borderColor },
          ticks: { color: textMutedColor },
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
  <div class="card flex h-full flex-col items-center">
    <div class="mb-4 text-xl font-semibold">{{ title }}</div>
    <div class="h-full w-full">
      <Chart type="polarArea" :data="data" :options="chartOptions" class="h-full w-full" />
    </div>
  </div>
</template>
