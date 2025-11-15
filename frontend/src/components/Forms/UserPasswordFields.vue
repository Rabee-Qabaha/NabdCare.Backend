<!-- src/components/Forms/UserPasswordFields.vue -->
<script setup lang="ts">
  import FloatLabel from 'primevue/floatlabel';
  import Password from 'primevue/password';
  import { computed, watch } from 'vue';

  import { usePasswordValidation } from '@/composables/validation/usePasswordValidation';
  import { getPasswordRequirementStatus } from '@/utils/users/userValidation';

  const props = withDefaults(
    defineProps<{
      mode: 'create' | 'reset' | 'change';
      loading?: boolean;
      submitted?: boolean;
    }>(),
    {
      mode: 'create',
      loading: false,
      submitted: false,
    },
  );

  const emit = defineEmits<{
    (e: 'update:passwords', value: any): void;
  }>();

  const {
    passwords,
    isPasswordSecure,
    isNewPasswordSecure,
    doPasswordsMatch,
    getFieldError,
    getFieldErrorMessage,
    markFieldTouched,
    resetPasswords,
  } = usePasswordValidation();

  // Keep parent synced with passwords
  watch(passwords, () => {
    emit('update:passwords', passwords);
  });

  const requiresCurrentPassword = computed(() => props.mode === 'change');

  const isValid = computed(() => {
    if (requiresCurrentPassword.value && !passwords.currentPassword) return false;

    return (
      passwords.newPassword &&
      passwords.confirmPassword &&
      isNewPasswordSecure.value &&
      doPasswordsMatch.value
    );
  });

  // Expose API to parent (same pattern ResetPasswordDialog uses)
  defineExpose({ resetPasswords, isValid });
</script>

<template>
  <div class="rounded-lg bg-surface-50 p-4 dark:bg-surface-800">
    <h4 class="mb-3 flex items-center gap-2 text-lg font-bold">
      <i class="pi pi-lock"></i>
      Password Settings
    </h4>

    <ul class="grid grid-cols-1 sm:grid-cols-2 mb-4 text-sm">
      <li
        v-for="req in getPasswordRequirementStatus(isPasswordSecure)"
        :key="req.key"
        class="flex items-center gap-2"
      >
        <i
          :class="req.met ? 'pi pi-check-circle text-green-500' : 'pi pi-times-circle text-red-500'"
        />
        {{ req.label }}
      </li>
    </ul>

    <div class="grid gap-5">
      <FloatLabel v-if="requiresCurrentPassword" variant="on">
        <Password
          id="currentPassword"
          v-model="passwords.currentPassword"
          toggleMask
          inputClass="w-full"
          class="w-full"
          :feedback="false"
          :disabled="props.loading"
        />
        <label for="currentPassword">Current Password *</label>
      </FloatLabel>

      <FloatLabel variant="on">
        <Password
          id="newPassword"
          v-model="passwords.newPassword"
          toggleMask
          inputClass="w-full"
          class="w-full"
          :feedback="true"
          :invalid="props.submitted && getFieldError('newPassword')"
          @input="markFieldTouched('newPassword')"
          :disabled="props.loading"
        />
        <label for="newPassword">New Password *</label>
      </FloatLabel>

      <small v-if="props.submitted && getFieldError('newPassword')" class="text-red-500 -mt-3">
        {{ getFieldErrorMessage('newPassword') }}
      </small>

      <FloatLabel variant="on">
        <Password
          id="confirmPassword"
          v-model="passwords.confirmPassword"
          toggleMask
          inputClass="w-full"
          class="w-full"
          :feedback="false"
          :invalid="props.submitted && getFieldError('confirmPassword')"
          @input="markFieldTouched('confirmPassword')"
          :disabled="props.loading"
        />
        <label for="confirmPassword">Confirm Password *</label>
      </FloatLabel>

      <small v-if="props.submitted && getFieldError('confirmPassword')" class="text-red-500 -mt-3">
        {{ getFieldErrorMessage('confirmPassword') }}
      </small>
    </div>
  </div>
</template>
