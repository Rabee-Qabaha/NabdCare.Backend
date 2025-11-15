// src/components/User/ChangePasswordDialog.vue
<script setup lang="ts">
  import UserPasswordFields from '@/components/Forms/UserPasswordFields.vue';
  import { useChangePassword } from '@/composables/query/users/useUserActions';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import { useToast } from 'primevue/usetoast';
  import { ref } from 'vue';

  const props = defineProps<{
    visible: boolean;
    userId: string;
  }>();

  const emit = defineEmits<{
    'update:visible': [boolean];
  }>();

  const toast = useToast();
  const mutation = useChangePassword();

  const passwordRef = ref<InstanceType<typeof UserPasswordFields> | null>(null);
  const passwordState = ref({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const submitted = ref(false);
  const isLoading = ref(false);

  async function onSave() {
    submitted.value = true;

    if (!passwordRef.value?.isValid) return;

    isLoading.value = true;
    try {
      await mutation.mutateAsync({
        id: props.userId,
        data: {
          currentPassword: passwordState.value.currentPassword,
          newPassword: passwordState.value.newPassword,
        },
      });

      toast.add({
        severity: 'success',
        summary: 'Password Updated',
        detail: 'Your password has been successfully changed.',
        life: 2000,
      });

      emit('update:visible', false);
    } finally {
      isLoading.value = false;
    }
  }
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Change Password"
    modal
    :style="{ width: '500px' }"
  >
    <UserPasswordFields
      ref="passwordRef"
      mode="change"
      :loading="isLoading"
      :submitted="submitted"
      @update:passwords="passwordState = $event"
    />

    <template #footer>
      <Button label="Cancel" severity="secondary" @click="emit('update:visible', false)" />
      <Button
        label="Change Password"
        icon="pi pi-save"
        :loading="isLoading"
        :disabled="!passwordRef?.isValid"
        @click="onSave"
      />
    </template>
  </Dialog>
</template>
