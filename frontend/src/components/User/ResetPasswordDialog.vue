<script setup lang="ts">
  import UserPasswordFields from '@/components/Forms/UserPasswordFields.vue';
  import { useUserActions } from '@/composables/query/users/useUserActions';
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import { computed, ref, watch } from 'vue';

  const props = defineProps<{
    visible: boolean;
    userId: string | null;
    fullName?: string;
    email?: string;
  }>();

  const emit = defineEmits<{
    'update:visible': [boolean];
  }>();

  const { resetPasswordMutation } = useUserActions();

  const passwordRef = ref<InstanceType<typeof UserPasswordFields> | null>(null);
  const passwordState = ref({
    newPassword: '',
    confirmPassword: '',
  });

  const submitted = ref(false);

  const isSubmitting = computed(() => resetPasswordMutation.isPending.value);

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  watch(
    () => props.visible,
    (open) => {
      if (open) {
        submitted.value = false;
        passwordState.value = { newPassword: '', confirmPassword: '' };
        passwordRef.value?.resetPasswords();
      }
    },
  );

  function onClose() {
    if (!isSubmitting.value) visible.value = false;
  }

  function onSave() {
    submitted.value = true;

    // Validate via child component
    if (!passwordRef.value?.isValid) return;
    if (!props.userId) return;

    resetPasswordMutation.mutate(
      {
        id: props.userId,
        data: { newPassword: passwordState.value.newPassword },
      },
      {
        onSuccess: () => {
          // Success toast is handled by useUserActions
          visible.value = false;
        },
      },
    );
  }
</script>

<template>
  <Dialog
    v-model:visible="visible"
    header="Reset User Password"
    modal
    :style="{ width: '500px', maxWidth: '95vw' }"
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
    @hide="onClose"
  >
    <div class="flex flex-col gap-5 pt-5">
      <div
        class="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-100 dark:border-blue-800/50 text-sm flex items-start gap-3"
      >
        <i class="pi pi-info-circle text-blue-600 dark:text-blue-400 mt-0.5 text-lg"></i>
        <div class="flex flex-col gap-1">
          <p class="text-blue-900 dark:text-blue-100 font-medium">
            Resetting password for:
            <span class="font-bold">{{ fullName || email || 'User' }}</span>
          </p>
          <p class="text-blue-700 dark:text-blue-300/80 text-xs leading-relaxed">
            As an admin, you can set a new temporary password. The user will be required to change
            it upon next login (if configured).
          </p>
        </div>
      </div>

      <UserPasswordFields
        ref="passwordRef"
        mode="reset"
        :loading="isSubmitting"
        :submitted="submitted"
        @update:passwords="Object.assign(passwordState, $event)"
      />
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 pt-5 w-full">
        <Button
          label="Cancel"
          severity="secondary"
          outlined
          :disabled="isSubmitting"
          @click="onClose"
        />
        <Button
          label="Reset Password"
          icon="pi pi-key"
          severity="danger"
          :loading="isSubmitting"
          @click="onSave"
        />
      </div>
    </template>
  </Dialog>
</template>
