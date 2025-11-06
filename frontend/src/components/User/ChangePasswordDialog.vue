<script setup lang="ts">
  import { watch, ref } from 'vue';
  import { useToast } from 'primevue/usetoast';
  import { useResetPassword } from '@/composables/query/users/useUserActions';
  import { usePasswordValidation } from '@/composables/validation/usePasswordValidation';
  import type { UserResponseDto } from '@/types/backend';

  // PrimeVue Components
  import Dialog from 'primevue/dialog';
  import Avatar from 'primevue/avatar';
  import Message from 'primevue/message';
  import Password from 'primevue/password';
  import Button from 'primevue/button';

  /**
   * Change Password Dialog Component
   * Location: src/components/User/ChangePasswordDialog.vue
   *
   * Purpose:
   * - Admin password reset for users
   * - Password security validation
   * - Vue Query mutation integration
   *
   * Features:
   * ✅ Real-time password strength checking
   * ✅ Comprehensive security requirements
   * ✅ Password confirmation matching
   * ✅ Loading states with disabled buttons
   * ✅ Error handling with user feedback
   * ✅ Shared password validation composable
   *
   * Author: Rabee Qabaha
   * Updated: 2025-11-04 20:29:53 UTC
   */

  // ========================================
  // PROPS & EMITS
  // ========================================

  const props = defineProps<{
    visible: boolean;
    user: Partial<UserResponseDto>;
  }>();

  const emit = defineEmits<{
    'update:visible': [value: boolean];
    save: [value: { newPassword: string }];
    cancel: [];
  }>();

  // ========================================
  // COMPOSABLES
  // ========================================

  const toast = useToast();
  const resetPasswordMutation = useResetPassword();

  // ✅ Use password validation composable
  const {
    passwords,
    fieldTouched,
    isPasswordSecure,
    isNewPasswordSecure,
    doPasswordsMatch,
    getFieldError,
    markFieldTouched,
    resetPasswords,
    getFieldErrorMessage,
  } = usePasswordValidation();

  // ========================================
  // STATE MANAGEMENT
  // ========================================

  const isLoading = ref(false);

  // ========================================
  // WATCHER - RESET FORM ON USER CHANGE
  // ========================================

  watch(
    () => props.user,
    () => {
      resetPasswords(); // ✅ Use composable reset
    },
    { deep: true },
  );

  // ========================================
  // PASSWORD VALIDATION
  // ========================================

  /**
   * Check if entire form is valid and ready to submit
   * Uses composable validation
   */
  // This is now handled by composable's doPasswordsMatch and isNewPasswordSecure

  // ========================================
  // HANDLERS
  // ========================================

  /**
   * Save password reset
   */
  async function onSave(): Promise<void> {
    // ✅ Mark all fields as touched
    markFieldTouched('newPassword');
    markFieldTouched('confirmPassword');

    // ✅ Use composable validation
    if (
      !passwords.newPassword ||
      !isNewPasswordSecure.value ||
      !passwords.confirmPassword ||
      !doPasswordsMatch.value
    ) {
      return;
    }

    isLoading.value = true;

    try {
      // Call mutation
      await resetPasswordMutation.mutateAsync({
        id: props.user.id!,
        data: { newPassword: passwords.newPassword } as any,
      });

      emit('save', { newPassword: passwords.newPassword });

      // ✅ Use composable reset
      resetPasswords();

      emit('update:visible', false);

      toast.add({
        severity: 'success',
        summary: 'Success',
        detail: 'Password reset successfully',
        life: 3000,
      });
    } catch (err: any) {
      toast.add({
        severity: 'error',
        summary: 'Error',
        detail: err.message || 'Failed to reset password',
        life: 3000,
      });
    } finally {
      isLoading.value = false;
    }
  }

  /**
   * Cancel and close dialog
   */
  function onCancel(): void {
    emit('cancel');
    emit('update:visible', false);
  }
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Reset User Password"
    :modal="true"
    :style="{ width: '500px' }"
    class="rounded-xl bg-surface-0 p-4 shadow-2xl dark:bg-surface-900"
  >
    <!-- User Info Card -->
    <div class="mb-4 rounded bg-blue-50 p-3 dark:bg-blue-900/20">
      <div class="flex items-center gap-3">
        <Avatar
          :label="user.fullName?.charAt(0) || '?'"
          shape="circle"
          style="background-color: var(--primary-color); color: white"
        />
        <div>
          <div class="text-900 font-semibold dark:text-white">
            {{ user.fullName }}
          </div>
          <div class="text-600 dark:text-400 text-sm">{{ user.email }}</div>
        </div>
      </div>
    </div>

    <!-- Info Message -->
    <Message severity="info" :closable="false" class="mb-4">
      As SuperAdmin, you can reset this user's password without knowing their current password.
    </Message>

    <!-- Password Fields -->
    <div class="flex flex-col gap-4">
      <div class="rounded-lg bg-surface-50 p-4 dark:bg-surface-800">
        <h4 class="text-900 mb-3 flex items-center gap-2 text-lg font-bold dark:text-white">
          <i class="pi pi-lock"></i>
          Set New Password
        </h4>
        <p class="text-600 dark:text-400 mb-3 text-sm">
          Password must meet the following security requirements:
        </p>

        <!-- Password Requirements Checklist -->
        <ul
          class="text-700 dark:text-300 m-0 mb-4 grid list-none grid-cols-1 gap-1 p-0 text-sm sm:grid-cols-2"
        >
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.minLength,
                'text-red-500': !isPasswordSecure.minLength,
              }"
            ></i>
            Minimum
            <span class="font-bold">12 characters</span>
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.uppercase,
                'text-red-500': !isPasswordSecure.uppercase,
              }"
            ></i>
            At least one
            <span class="font-bold">uppercase</span>
            letter
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.lowercase,
                'text-red-500': !isPasswordSecure.lowercase,
              }"
            ></i>
            At least one
            <span class="font-bold">lowercase</span>
            letter
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.digit,
                'text-red-500': !isPasswordSecure.digit,
              }"
            ></i>
            At least one
            <span class="font-bold">digit</span>
            (0-9)
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.specialChar,
                'text-red-500': !isPasswordSecure.specialChar,
              }"
            ></i>
            At least one
            <span class="font-bold">special character</span>
          </li>
        </ul>

        <div class="grid gap-4">
          <!-- New Password Input -->
          <div>
            <label for="newPassword" class="mb-2 block font-semibold dark:text-white">
              New Password
              <span class="text-red-500">*</span>
            </label>
            <Password
              id="newPassword"
              v-model="passwords.newPassword"
              toggleMask
              :feedback="true"
              class="w-full"
              inputClass="w-full"
              placeholder="Enter new password"
              @input="markFieldTouched('newPassword')"
              :invalid="getFieldError('newPassword')"
              :disabled="isLoading"
            />
            <small v-if="getFieldError('newPassword')" class="text-red-500">
              {{ getFieldErrorMessage('newPassword') }}
            </small>
          </div>

          <!-- Confirm Password Input -->
          <div>
            <label for="confirmPassword" class="mb-2 block font-semibold dark:text-white">
              Confirm New Password
              <span class="text-red-500">*</span>
            </label>
            <Password
              id="confirmPassword"
              v-model="passwords.confirmPassword"
              toggleMask
              :feedback="false"
              class="w-full"
              inputClass="w-full"
              placeholder="Re-enter new password"
              @input="markFieldTouched('confirmPassword')"
              :invalid="getFieldError('confirmPassword')"
              :disabled="isLoading"
            />
            <small v-if="getFieldError('confirmPassword')" class="text-red-500">
              {{ getFieldErrorMessage('confirmPassword') }}
            </small>
          </div>
        </div>
      </div>
    </div>

    <!-- Dialog Footer -->
    <template #footer>
      <Button
        label="Cancel"
        icon="pi pi-times"
        severity="secondary"
        outlined
        @click="onCancel"
        :disabled="isLoading"
      />
      <Button
        label="Reset Password"
        icon="pi pi-key"
        severity="warning"
        @click="onSave"
        :disabled="
          !passwords.newPassword ||
          !isNewPasswordSecure ||
          !passwords.confirmPassword ||
          !doPasswordsMatch ||
          isLoading
        "
        :loading="isLoading"
      />
    </template>
  </Dialog>
</template>
