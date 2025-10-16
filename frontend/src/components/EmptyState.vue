<template>
    <div class="flex flex-col items-center justify-center text-center p-6 text-muted-color gap-3" :class="containerClass">
        <!-- Icon -->
        <i v-if="icon" :class="icon + ' text-5xl opacity-60'"></i>

        <!-- Title -->
        <div v-if="title" class="text-lg font-semibold">
            {{ title }}
        </div>

        <!-- Description -->
        <div v-if="description" class="text-sm opacity-80">
            {{ description }}
        </div>

        <!-- Optional action slot (like a button) -->
        <div v-if="$slots.action">
            <slot name="action" />
        </div>
    </div>
</template>

<script setup lang="ts">
    import { toRefs } from 'vue';

    const props = defineProps<{
        icon?: string;
        title?: string;
        description?: string;
        containerClass?: string;
    }>();

    const { icon, title, description, containerClass = '' } = toRefs(props);
</script>
