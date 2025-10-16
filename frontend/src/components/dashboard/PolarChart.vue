<script setup>
    defineProps({
        title: { type: String, default: 'Polar Area' },
        data: { type: Object, required: true },
        options: { type: Object, required: true }
    });
</script>

<template>
    <div class="card flex flex-col items-center">
        <div class="font-semibold text-xl mb-4">{{ title }}</div>
        <Chart type="polarArea" :data="data" :options="options" />
    </div>
</template>
