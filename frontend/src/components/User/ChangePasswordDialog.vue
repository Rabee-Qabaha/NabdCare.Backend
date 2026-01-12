<script setup lang="ts">
  import UserPasswordFields from '@/components/Forms/UserPasswordFields.vue';
  import { useUserActions } from '@/composables/query/users/useUserActions';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    visible: boolean;
    userId: string;
  }>();

  const emit = defineEmits<{
    'update:visible': [boolean];
  }>();

  // 1. Use Actions
  const { changePasswordMutation } = useUserActions();

  // 2. State
  const passwordRef = ref<InstanceType<typeof UserPasswordFields> | null>(null);
  const passwordState = ref({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const submitted = ref(false);

  // Use the mutation's loading state
  const isSubmitting = computed(() => changePasswordMutation.isPending.value);

  function onSave() {
    submitted.value = true;

    // Check validity exposed by child component
    if (!passwordRef.value?.isValid) return;

    // 3. Execute Mutation
    changePasswordMutation.mutate(
      {
        id: props.userId,
        data: {
          currentPassword: passwordState.value.currentPassword,
          newPassword: passwordState.value.newPassword,
        },
      },
      {
        onSuccess: () => {
          // Success Toast is handled in useUserActions
          emit('update:visible', false);

          // Optional: Reset form
          passwordState.value = { currentPassword: '', newPassword: '', confirmPassword: '' };
          submitted.value = false;
        },
        // Error Toast is handled by Global Interceptor
      },
    );
  }

  function onClose() {
    if (!isSubmitting.value) emit('update:visible', false);
  }
</script>

<template>
  <Dialog
    :visible="visible"
    header="Change Password"
    modal
    :style="{ width: '500px' }"
    :closable="!isSubmitting"
    :pt="{
      root: { class: 'rounded-xl border-0 shadow-2xl overflow-hidden' },
      header: {
        class:
          'border-b border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-white dark:bg-surface-900',
      },
      content: { class: 'p-6 bg-white dark:bg-surface-900' },
      footer: {
        class:
          'border-t border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
    }"
    @update:visible="emit('update:visible', $event)"
  >
    <div class="pt-5">
      <UserPasswordFields
        ref="passwordRef"
        mode="change"
        :loading="isSubmitting"
        :submitted="submitted"
        @update:passwords="Object.assign(passwordState, $event)"
      />
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 w-full pt-5">
        <Button
          label="Cancel"
          severity="secondary"
          outlined
          :disabled="isSubmitting"
          @click="onClose"
        />
        <Button label="Change Password" icon="pi pi-save" :loading="isSubmitting" @click="onSave" />
      </div>
    </template>
  </Dialog>
</template>
