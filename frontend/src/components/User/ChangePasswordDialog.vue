<script setup lang="ts">
import { reactive, computed, watch, ref } from "vue";
import { useToast } from "primevue/usetoast";
import { useResetPassword } from "@/composables/query/users/useUserActions";
import type { UserResponseDto } from "@/types/backend";

// ... existing imports ...

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
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

// ========================================
// PROPS & EMITS
// ========================================

const props = defineProps<{
  visible: boolean;
  user: Partial<UserResponseDto>;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  save: [value: { newPassword: string }];
  cancel: [];
}>();

// ========================================
// COMPOSABLES
// ========================================

const toast = useToast();
const resetPasswordMutation = useResetPassword();

// ========================================
// STATE MANAGEMENT
// ========================================

const localPasswords = reactive({
  newPassword: "",
  confirmPassword: "",
});

const fieldTouched = reactive({
  newPassword: false,
  confirmPassword: false,
});

// ✅ FIXED: Use local loading state instead of mutation.isPending
const isLoading = ref(false);

// ========================================
// WATCHER - RESET FORM ON USER CHANGE
// ========================================

watch(
  () => props.user,
  () => {
    localPasswords.newPassword = "";
    localPasswords.confirmPassword = "";
    fieldTouched.newPassword = false;
    fieldTouched.confirmPassword = false;
  },
  { deep: true }
);

// ========================================
// PASSWORD VALIDATION
// ========================================

/**
 * Check individual password security requirements
 */
const isPasswordSecure = computed(() => {
  const password = localPasswords.newPassword;
  return {
    minLength: password.length >= 12,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    digit: /\d/.test(password),
    specialChar: /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password),
  };
});

/**
 * Check if password meets all security requirements
 */
const isNewPasswordSecure = computed(() => {
  const security = isPasswordSecure.value;
  return (
    security.minLength &&
    security.uppercase &&
    security.lowercase &&
    security.digit &&
    security.specialChar
  );
});

/**
 * Get validation errors for a field
 */
function getError(key: "newPassword" | "confirmPassword"): boolean {
  if (!fieldTouched[key]) return false;

  if (key === "newPassword") {
    return !localPasswords.newPassword || !isNewPasswordSecure.value;
  }

  if (key === "confirmPassword") {
    return (
      !localPasswords.confirmPassword ||
      localPasswords.confirmPassword !== localPasswords.newPassword
    );
  }

  return false;
}

/**
 * Check if entire form is valid and ready to submit
 */
const isFormValid = computed(() => {
  return (
    localPasswords.newPassword &&
    isNewPasswordSecure.value &&
    localPasswords.confirmPassword &&
    localPasswords.newPassword === localPasswords.confirmPassword
  );
});

// ========================================
// HANDLERS
// ========================================

/**
 * Save password reset
 * ✅ FIXED: Proper loading state management
 */
async function onSave(): Promise<void> {
  fieldTouched.newPassword = true;
  fieldTouched.confirmPassword = true;

  if (!isFormValid.value) return;

  // ✅ FIXED: Set local loading state
  isLoading.value = true;

  try {
    // ✅ Call mutation
    const result = await resetPasswordMutation.mutateAsync({
      id: props.user.id!,
      data: { newPassword: localPasswords.newPassword } as any,
    });

    console.log("✅ Password reset successful:", result);

    emit("save", { newPassword: localPasswords.newPassword });

    // ✅ Reset form before closing
    localPasswords.newPassword = "";
    localPasswords.confirmPassword = "";
    fieldTouched.newPassword = false;
    fieldTouched.confirmPassword = false;

    emit("update:visible", false);
  } catch (err: any) {
    console.error("❌ Password reset failed:", err);

    toast.add({
      severity: "error",
      summary: "Error",
      detail: err.message || "Failed to reset password",
      life: 3000,
    });
  } finally {
    // ✅ FIXED: Clear loading state in finally block
    isLoading.value = false;
  }
}

/**
 * Cancel and close dialog
 */
function onCancel(): void {
  emit("cancel");
  emit("update:visible", false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Reset User Password"
    :modal="true"
    :style="{ width: '500px' }"
    class="p-4 bg-surface-0 dark:bg-surface-900 shadow-2xl rounded-xl"
  >
    <!-- User Info Card -->
    <div class="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded">
      <div class="flex align-items-center gap-3">
        <Avatar
          :label="user.fullName?.charAt(0) || '?'"
          shape="circle"
          style="background-color: var(--primary-color); color: white"
        />
        <div>
          <div class="font-semibold text-900 dark:text-white">
            {{ user.fullName }}
          </div>
          <div class="text-sm text-600 dark:text-400">{{ user.email }}</div>
        </div>
      </div>
    </div>

    <!-- Info Message -->
    <Message severity="info" :closable="false" class="mb-4">
      As SuperAdmin, you can reset this user's password without knowing their
      current password.
    </Message>

    <!-- Password Fields -->
    <div class="flex flex-col gap-4">
      <div class="p-4 bg-surface-50 dark:bg-surface-800 rounded-lg">
        <h4
          class="text-lg font-bold text-900 dark:text-white mb-3 flex items-center gap-2"
        >
          <i class="pi pi-lock"></i> Set New Password
        </h4>
        <p class="text-sm text-600 dark:text-400 mb-3">
          Password must meet the following security requirements:
        </p>

        <!-- Password Requirements Checklist -->
        <ul
          class="list-none p-0 m-0 text-sm text-700 dark:text-300 grid grid-cols-1 sm:grid-cols-2 gap-1 mb-4"
        >
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.minLength,
                'text-red-500': !isPasswordSecure.minLength,
              }"
            ></i>
            Minimum <span class="font-bold">12 characters</span>
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.uppercase,
                'text-red-500': !isPasswordSecure.uppercase,
              }"
            ></i>
            At least one <span class="font-bold">uppercase</span> letter
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.lowercase,
                'text-red-500': !isPasswordSecure.lowercase,
              }"
            ></i>
            At least one <span class="font-bold">lowercase</span> letter
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.digit,
                'text-red-500': !isPasswordSecure.digit,
              }"
            ></i>
            At least one <span class="font-bold">digit</span> (0-9)
          </li>
          <li class="flex items-center gap-2">
            <i
              class="pi pi-check-circle"
              :class="{
                'text-green-500': isPasswordSecure.specialChar,
                'text-red-500': !isPasswordSecure.specialChar,
              }"
            ></i>
            At least one <span class="font-bold">special character</span>
          </li>
        </ul>

        <div class="grid gap-4">
          <!-- New Password Input -->
          <div class="col-12">
            <label
              for="newPassword"
              class="block font-semibold mb-2 dark:text-white"
              >New Password <span class="text-red-500">*</span></label
            >
            <Password
              id="newPassword"
              v-model="localPasswords.newPassword"
              toggleMask
              :feedback="true"
              class="w-full"
              inputClass="w-full"
              placeholder="Enter new password"
              @input="fieldTouched.newPassword = true"
              :invalid="getError('newPassword')"
              :disabled="isLoading"
            />
            <small
              v-if="getError('newPassword') && localPasswords.newPassword"
              class="text-red-500"
            >
              Password does not meet all security requirements.
            </small>
            <small v-else-if="getError('newPassword')" class="text-red-500">
              New password is required.
            </small>
          </div>

          <!-- Confirm Password Input -->
          <div class="col-12">
            <label
              for="confirmPassword"
              class="block font-semibold mb-2 dark:text-white"
              >Confirm New Password <span class="text-red-500">*</span></label
            >
            <Password
              id="confirmPassword"
              v-model="localPasswords.confirmPassword"
              toggleMask
              :feedback="false"
              class="w-full"
              inputClass="w-full"
              placeholder="Re-enter new password"
              @input="fieldTouched.confirmPassword = true"
              :invalid="getError('confirmPassword')"
              :disabled="isLoading"
            />
            <small
              v-if="
                getError('confirmPassword') && !localPasswords.confirmPassword
              "
              class="text-red-500"
            >
              Password confirmation is required.
            </small>
            <small v-else-if="getError('confirmPassword')" class="text-red-500">
              Passwords do not match.
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
        :disabled="!isFormValid || isLoading"
        :loading="isLoading"
      />
    </template>
  </Dialog>
</template>
