<script setup lang="ts">
    import { ref, watch, onMounted } from 'vue';
    import { useLayout } from '@/layout/composables/layout';

    const props = defineProps({
        title: { type: String, default: 'Visits Comparison' },
        rawData: { type: Object, required: true } // Dashboard يمرر بس labels + datasets بدون colors
    });

    const { getPrimary, getSurface, isDarkTheme } = useLayout();
    const chartData = ref<any>({});
    const chartOptions = ref<any>({});

    // --- Chart data with theme colors ---
    function setChartData(rawData: any) {
        if (!rawData || !rawData.labels || !rawData.datasets || rawData.datasets.length === 0) {
            return { labels: [], datasets: [] };
        }

        const style = getComputedStyle(document.documentElement);
        const primary500 = style.getPropertyValue('--p-primary-500');
        const primary300 = style.getPropertyValue('--p-primary-300');

        // handle dataset[0] & dataset[1] safely
        const ds1 = rawData.datasets[0] || { label: 'Dataset 1', data: [] };
        const ds2 = rawData.datasets[1] || { label: 'Dataset 2', data: [] };

        return {
            labels: rawData.labels,
            datasets: [
                {
                    label: ds1.label,
                    data: ds1.data,
                    backgroundColor: primary500,
                    borderRadius: { topLeft: 8, topRight: 8 },
                    barThickness: 32
                },
                {
                    label: ds2.label,
                    data: ds2.data,
                    backgroundColor: primary300,
                    borderRadius: { topLeft: 8, topRight: 8 },
                    barThickness: 32
                }
            ]
        };
    }

    // --- Chart options with theme colors ---
    function setChartOptions() {
        if (!props.rawData?.labels || !props.rawData?.datasets) {
            return { labels: [], datasets: [] };
        }
        const style = getComputedStyle(document.documentElement);
        const borderColor = style.getPropertyValue('--surface-border');
        const textColor = style.getPropertyValue('--text-color');
        const textMutedColor = style.getPropertyValue('--text-color-secondary');

        return {
            maintainAspectRatio: false,
            aspectRatio: 0.8,
            plugins: {
                legend: { labels: { color: textColor } },
                tooltip: {
                    mode: 'nearest',
                    intersect: true
                }
            },
            scales: {
                x: {
                    stacked: false,
                    ticks: { color: textMutedColor },
                    grid: { color: 'transparent', borderColor: 'transparent' },
                    offset: true,
                    categoryPercentage: 0.6,
                    barPercentage: 0.9
                },
                y: {
                    stacked: false,
                    ticks: { color: textMutedColor },
                    grid: { color: borderColor, borderColor: 'transparent', drawTicks: false }
                }
            }
        };
    }

    // --- Watch theme changes ---
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
    <div class="card h-full flex flex-col">
        <div class="font-semibold text-xl mb-4">{{ title }}</div>
        <div class="flex-1">
            <Chart type="bar" :data="chartData" :options="chartOptions" class="w-full h-full" />
        </div>
    </div>
</template>
