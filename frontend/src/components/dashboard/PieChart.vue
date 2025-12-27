<!-- <script setup lang="ts">
  import { ref, watch, onMounted } from 'vue';
  import { useLayout } from '@/layout/composables/layout';
  import { formatCurrency } from '@/utils/uiHelpers';

  const props = defineProps({
    title: { type: String, default: 'Chart' },
    data: { type: Object, required: true },
    showCurrency: { type: Boolean, default: false },
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
              return props.showCurrency ? ` ${formatCurrency(value)}` : ` ${value}`;
            },
          },
        },
      },
    };
  }

  // Update chart options on theme change
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
    <div class="w-full flex-1">
      <Chart type="pie" :data="data" :options="chartOptions" class="h-full w-full" />
    </div>
  </div>
</template> -->
