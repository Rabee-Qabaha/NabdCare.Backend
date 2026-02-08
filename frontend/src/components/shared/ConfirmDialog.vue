<script setup lang="ts">
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';

  defineProps<{
    visible: boolean;
    title: string;
    message: string;
    severity?: 'danger' | 'info' | 'warn' | 'success';
    confirmLabel?: string;
    cancelLabel?: string;
  }>();

  const emit = defineEmits(['update:visible', 'confirm', 'cancel']);

  const handleConfirm = () => {
    emit('confirm');
    emit('update:visible', false);
  };

  const handleCancel = () => {
    emit('cancel');
    emit('update:visible', false);
  };
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :header="title"
    :style="{ width: '90vw', maxWidth: '400px' }"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="flex flex-col gap-4">
      <p class="m-0 text-surface-600 dark:text-surface-300">{{ message }}</p>
      <div class="flex justify-end gap-2">
        <Button :label="cancelLabel || 'Cancel'" text severity="secondary" @click="handleCancel" />
        <Button
          :label="confirmLabel || 'Confirm'"
          :severity="severity || 'danger'"
          autofocus
          @click="handleConfirm"
        />
      </div>
    </div>
  </Dialog>
</template>
