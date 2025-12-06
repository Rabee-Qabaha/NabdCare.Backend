<template>
  <Dialog
    v-model:visible="visible"
    modal
    :style="{ width: '400px' }"
    :showHeader="false"
    :closable="!loading"
    :dismissableMask="!loading"
    class="rounded-xl overflow-hidden shadow-2xl"
    :contentClass="'!p-0 !rounded-xl border border-surface-200 dark:border-surface-700 bg-white dark:bg-surface-900'"
    @hide="onClose"
  >
    <div class="relative flex flex-col items-center p-6 text-center">
      <Button
        icon="pi pi-times"
        text
        rounded
        size="small"
        class="!absolute right-3 top-3 !text-surface-400 hover:!text-surface-600 dark:hover:!text-surface-200 !w-8 !h-8"
        @click="onClose"
        :disabled="loading"
      />

      <div
        class="mb-4 flex h-16 w-16 items-center justify-center rounded-full border-4"
        :class="[
          mode === 'hard'
            ? 'border-red-50 bg-red-100 text-red-600 dark:border-red-900/30 dark:bg-red-900/50 dark:text-red-400'
            : 'border-orange-50 bg-orange-100 text-orange-600 dark:border-orange-900/30 dark:bg-orange-900/50 dark:text-orange-400',
        ]"
      >
        <i
          class="pi text-2xl"
          :class="[mode === 'hard' ? 'pi-exclamation-triangle' : 'pi-trash']"
        ></i>
      </div>

      <h3 class="mb-2 text-xl font-bold text-surface-900 dark:text-surface-0">
        {{ title || (mode === 'hard' ? 'Permanently Delete?' : 'Move to Trash?') }}
      </h3>

      <p class="mb-6 text-sm leading-relaxed text-surface-500 dark:text-surface-400">
        {{ message }}
        <span
          v-if="itemIdentifier"
          class="font-bold text-surface-900 dark:text-surface-200 block mt-1"
        >
          "{{ itemIdentifier }}"
        </span>
      </p>

      <div
        v-if="mode === 'hard'"
        class="mb-6 flex w-full gap-3 rounded-lg border border-red-200 bg-red-50/50 p-3 text-left dark:border-red-900/50 dark:bg-red-900/10"
      >
        <i class="pi pi-info-circle mt-0.5 shrink-0 text-red-600 dark:text-red-400"></i>
        <div class="text-xs text-red-900 dark:text-red-300">
          <span class="mb-0.5 block font-bold">This action cannot be undone.</span>
          This data will be wiped from the database forever.
        </div>
      </div>

      <div
        v-if="mode === 'soft'"
        class="mb-6 flex w-full gap-3 rounded-lg border border-surface-200 bg-surface-50 p-3 text-left dark:border-surface-700 dark:bg-surface-800/30"
      >
        <i class="pi pi-info-circle mt-0.5 shrink-0 text-surface-500"></i>
        <div class="text-xs text-surface-600 dark:text-surface-300">
          You can restore this item anytime from the
          <span class="font-bold text-surface-900 dark:text-surface-0">Deleted</span>
          filter.
        </div>
      </div>

      <div class="flex w-full gap-3">
        <Button
          label="Cancel"
          severity="secondary"
          class="flex-1 !bg-surface-100 !text-surface-600 hover:!bg-surface-200 dark:!bg-surface-800 dark:!text-surface-300 dark:hover:!bg-surface-700 !border-0"
          @click="onClose"
          :disabled="loading"
        />
        <Button
          :label="confirmLabelComputed"
          :severity="mode === 'hard' ? 'danger' : 'warn'"
          class="flex-1"
          :loading="loading"
          @click="onConfirm"
        />
      </div>
    </div>
  </Dialog>
</template>

<script setup lang="ts">
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import { computed } from 'vue';

  interface Props {
    visible: boolean;
    loading?: boolean;
    mode?: 'soft' | 'hard';
    title?: string;
    message?: string;
    itemIdentifier?: string;
    confirmLabel?: string;
  }

  const props = withDefaults(defineProps<Props>(), {
    visible: false,
    loading: false,
    mode: 'soft',
    message: 'Are you sure you want to delete',
  });

  const emit = defineEmits<{
    'update:visible': [value: boolean];
    confirm: [];
  }>();

  const visible = computed({
    get: () => props.visible,
    set: (val) => emit('update:visible', val),
  });

  const confirmLabelComputed = computed(() => {
    if (props.confirmLabel) return props.confirmLabel;
    return props.mode === 'hard' ? 'Delete Forever' : 'Delete';
  });

  const onClose = () => {
    visible.value = false;
  };

  const onConfirm = () => {
    emit('confirm');
  };
</script>
