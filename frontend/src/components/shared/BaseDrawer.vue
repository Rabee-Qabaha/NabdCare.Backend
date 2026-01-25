<script setup lang="ts">
  import Button from 'primevue/button';
  import Drawer from 'primevue/drawer';

  withDefaults(
    defineProps<{
      visible: boolean;
      title?: string;
      subtitle?: string;
      icon?: string;
      width?: string;
      noPadding?: boolean;
    }>(),
    {
      width: 'md:!w-[480px]',
      noPadding: false,
    },
  );

  const emit = defineEmits<{
    (e: 'update:visible', value: boolean): void;
    (e: 'close'): void;
  }>();

  const closeDrawer = () => {
    emit('update:visible', false);
    emit('close');
  };
</script>

<template>
  <Drawer
    :visible="visible"
    @update:visible="(v) => emit('update:visible', v)"
    position="right"
    :modal="true"
    :dismissable="true"
    :showCloseIcon="false"
    :class="['!w-full !border-0 shadow-2xl', width]"
    :pt="{
      root: { class: '!bg-surface-0 dark:!bg-surface-900' },
      header: { class: '!hidden' },
      content: { class: '!p-0 !h-full' },
    }"
  >
    <div class="flex flex-col h-full bg-surface-0 dark:bg-surface-900">
      <div
        class="shrink-0 px-6 py-5 border-b border-surface-200 dark:border-surface-800 flex items-start justify-between bg-surface-0/80 dark:bg-surface-900/80 backdrop-blur-md z-10 sticky top-0"
      >
        <div class="flex gap-3 items-center">
          <div
            v-if="icon"
            class="w-10 h-10 rounded-xl bg-surface-100 dark:bg-surface-800 flex items-center justify-center text-surface-500 dark:text-surface-400"
          >
            <i :class="['text-xl', icon]"></i>
          </div>

          <div>
            <slot name="header">
              <h2 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-none">
                {{ title }}
              </h2>
              <p v-if="subtitle" class="mt-1 text-sm text-surface-500 dark:text-surface-400">
                {{ subtitle }}
              </p>
            </slot>
          </div>
        </div>

        <Button
          icon="pi pi-times"
          text
          rounded
          severity="secondary"
          aria-label="Close"
          class="!w-8 !h-8 !text-surface-500 hover:!bg-surface-100 dark:hover:!bg-surface-800"
          @click="closeDrawer"
        />
      </div>

      <div
        class="flex-grow overflow-y-auto relative [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:bg-surface-200 dark:[&::-webkit-scrollbar-thumb]:bg-surface-700 [&::-webkit-scrollbar-thumb]:rounded-full hover:[&::-webkit-scrollbar-thumb]:bg-surface-300 dark:hover:[&::-webkit-scrollbar-thumb]:bg-surface-600"
        :class="{ 'p-6': !noPadding }"
      >
        <slot />
      </div>

      <div
        v-if="$slots.footer"
        class="shrink-0 px-6 py-4 bg-surface-50 dark:bg-surface-950 border-t border-surface-200 dark:border-surface-800 flex items-center justify-end gap-3"
      >
        <slot name="footer" :close="closeDrawer" />
      </div>
    </div>
  </Drawer>
</template>
