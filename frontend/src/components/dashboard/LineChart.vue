<script setup lang="ts">
  import { ref, watch, onMounted } from 'vue';
  import { useLayout } from '@/layout/composables/layout';
  import { formatCurrency } from '@/utils/uiHelpers';

  const props = defineProps({
    title: { type: String, default: 'Line Chart' },
    rawData: { type: Object, required: true },
  });

  const { getPrimary, getSurface, isDarkTheme } = useLayout();
  const chartData = ref<any>({});
  const chartOptions = ref<any>({});

  // --- Chart data مع ألوان الـ theme ---
  function setChartData(rawData: any) {
    if (!rawData || !rawData.labels || !rawData.datasets || rawData.datasets.length < 3) {
      return { labels: [], datasets: [] };
    }

    const style = getComputedStyle(document.documentElement);
    const primary500 = style.getPropertyValue('--p-primary-500');
    const primary700 = style.getPropertyValue('--p-primary-700');
    const danger = '#f87171';

    const ds1 = rawData.datasets[0] || { label: 'Dataset 1', data: [] };
    const ds2 = rawData.datasets[1] || { label: 'Dataset 2', data: [] };
    const ds3 = rawData.datasets[2] || { label: 'Dataset 3', data: [] };

    return {
      labels: rawData.labels,
      datasets: [
        {
          label: ds1.label,
          data: ds1.data,
          borderColor: primary500,
          pointBackgroundColor: primary500,
          pointBorderColor: '#fff',
          tension: 0.3,
          fill: true,
        },
        {
          label: ds2.label,
          data: ds2.data,
          borderColor: primary700,
          pointBackgroundColor: primary700,
          pointBorderColor: '#fff',
          tension: 0.3,
          fill: true,
        },
        {
          label: ds3.label,
          data: ds3.data,
          borderColor: danger,
          pointBackgroundColor: danger,
          pointBorderColor: '#fff',
          tension: 0.3,
          fill: true,
          borderDash: [5, 5],
        },
      ],
    };
  }

  // --- Chart options ---
  function setChartOptions() {
    const style = getComputedStyle(document.documentElement);
    const borderColor = style.getPropertyValue('--surface-border');
    const textMutedColor = style.getPropertyValue('--text-color-secondary');
    const textColor = style.getPropertyValue('--text-color');

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
              const value = context.raw ?? 0;
              return `${context.dataset.label}: ${formatCurrency(value)}`;
            },
          },
        },
      },
      scales: {
        x: {
          ticks: { color: textMutedColor, font: { weight: 500 } },
          grid: { color: borderColor, drawBorder: false },
        },
        y: {
          ticks: { color: textMutedColor },
          grid: { color: borderColor, drawBorder: false },
        },
      },
    };
  }

  // --- Watch on theme changes ---
  watch([getPrimary, getSurface, isDarkTheme], () => {
    chartData.value = setChartData(<any>props.rawData);
    chartOptions.value = setChartOptions();
  });

  onMounted(() => {
    chartData.value = setChartData(<any>props.rawData);
    chartOptions.value = setChartOptions();
  });
</script>

<template>
  <div class="card flex h-full flex-col">
    <div class="mb-4 text-xl font-semibold">{{ title }}</div>
    <div class="flex-1">
      <Chart type="line" :data="chartData" :options="chartOptions" class="h-full w-full" />
    </div>
  </div>
</template>
