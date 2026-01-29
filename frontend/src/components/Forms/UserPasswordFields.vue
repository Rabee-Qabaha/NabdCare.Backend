<script setup lang="ts">
  import FloatLabel from 'primevue/floatlabel';
  import Password from 'primevue/password';
  import { computed, watch } from 'vue';

  import { usePasswordValidation } from '@/composables/validation/usePasswordValidation';
  import { getPasswordRequirementStatus, validatePassword } from '@/utils/users/userValidation';

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
    isNewPasswordSecure, // Boolean
    doPasswordsMatch,
    getFieldError,
    getFieldErrorMessage,
    markFieldTouched,
    markAllFieldsTouched,
    resetPasswords,
  } = usePasswordValidation();

  // Keep parent synced with passwords
  watch(passwords, () => {
    emit('update:passwords', passwords);
  });

  watch(
    () => props.submitted,
    (val) => {
      if (val) markAllFieldsTouched();
    },
    { immediate: true },
  );

  const requiresCurrentPassword = computed(() => props.mode === 'change');

  const requirementsList = computed(() => {
    const strength = validatePassword(passwords.newPassword || '');
    return getPasswordRequirementStatus(strength);
  });

  const isValid = computed(() => {
    if (requiresCurrentPassword.value && !passwords.currentPassword) return false;

    return (
      passwords.newPassword &&
      passwords.confirmPassword &&
      isNewPasswordSecure.value &&
      doPasswordsMatch.value
    );
  });

  // Expose API to parent
  defineExpose({ resetPasswords, isValid });
</script>

<template>
  <div class="rounded-lg bg-surface-50 p-4 dark:bg-surface-800">
    <h4 class="mb-3 flex items-center gap-2 text-lg font-bold text-surface-900 dark:text-surface-0">
      <i class="pi pi-lock"></i>
      Password Settings
    </h4>

    <ul class="grid grid-cols-1 sm:grid-cols-2 mb-4 text-sm gap-2">
      <li v-for="req in requirementsList" :key="req.label" class="flex items-center gap-2">
        <i
          class="pi text-[10px]"
          :class="req.met ? 'pi-check-circle text-green-500' : 'pi-times-circle text-red-500'"
        />
        <span
          :class="{
            'text-surface-900 dark:text-surface-0': req.met,
            'text-surface-600 dark:text-surface-400': !req.met,
          }"
        >
          {{ req.label }}
        </span>
      </li>
    </ul>

    <div class="grid gap-5">
      <FloatLabel v-if="requiresCurrentPassword" variant="on">
        <Password
          id="currentPassword"
          v-model="passwords.currentPassword"
          toggle-mask
          input-class="w-full"
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
          toggle-mask
          input-class="w-full"
          class="w-full"
          :feedback="true"
          :invalid="props.submitted && !!getFieldError('newPassword')"
          :disabled="props.loading"
          @input="markFieldTouched('newPassword')"
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
          toggle-mask
          input-class="w-full"
          class="w-full"
          :feedback="false"
          :invalid="props.submitted && !!getFieldError('confirmPassword')"
          :disabled="props.loading"
          @input="markFieldTouched('confirmPassword')"
        />
        <label for="confirmPassword">Confirm Password *</label>
      </FloatLabel>

      <small v-if="props.submitted && getFieldError('confirmPassword')" class="text-red-500 -mt-3">
        {{ getFieldErrorMessage('confirmPassword') }}
      </small>
    </div>
  </div>
</template>
