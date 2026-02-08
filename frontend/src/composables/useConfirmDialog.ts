import { ref } from 'vue';

export function useConfirmDialog() {
  const confirmDialogProps = ref({
    visible: false,
    title: '',
    message: '',
    severity: 'danger' as 'danger' | 'info' | 'warn' | 'success',
    confirmLabel: 'Confirm',
    onConfirm: () => {},
    onCancel: () => {},
  });

  const showConfirm = (options: {
    title: string;
    message: string;
    severity?: 'danger' | 'info' | 'warn' | 'success';
    confirmLabel?: string;
    onConfirm: () => void;
    onCancel?: () => void;
  }) => {
    confirmDialogProps.value = {
      visible: true,
      title: options.title,
      message: options.message,
      severity: options.severity || 'danger',
      confirmLabel: options.confirmLabel || 'Confirm',
      onConfirm: options.onConfirm,
      onCancel: options.onCancel || (() => {}),
    };
  };

  const handleConfirm = () => {
    confirmDialogProps.value.onConfirm();
    confirmDialogProps.value.visible = false;
  };

  const handleCancel = () => {
    confirmDialogProps.value.onCancel();
    confirmDialogProps.value.visible = false;
  };

  return {
    showConfirm,
    confirmDialogProps,
    handleConfirm,
    handleCancel,
  };
}
