<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Reset User Password"
    :modal="true"
    :style="{ width: '500px' }"
    class="p-4 bg-surface-0 dark:bg-surface-900 shadow-2xl rounded-xl"
  >
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

    <Message severity="info" :closable="false" class="mb-4">
      As SuperAdmin, you can reset this user's password without knowing their
      current password.
    </Message>

    <div class="flex flex-col gap-4">
      <!-- Security Requirements and Password Fields Container -->
      <div class="p-4 bg-surface-50 dark:bg-surface-800 rounded-lg">
        <h4
          class="text-lg font-bold text-900 dark:text-white mb-3 flex items-center gap-2"
        >
          Set New Password
        </h4>
        <p class="text-sm text-600 dark:text-400 mb-3">
          Password must meet the following security requirements:
        </p>

        <!-- Password Requirements List (Updated for correct bolding) -->
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
          <!-- New Password -->
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

          <!-- Confirm Password -->
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

    <template #footer>
      <Button
        label="Cancel"
        icon="pi pi-times"
        severity="secondary"
        outlined
        @click="onCancel"
      />
      <Button
        label="Reset Password"
        icon="pi pi-key"
        severity="warning"
        @click="onSave"
        :disabled="!isFormValid"
        :loading="saving"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { reactive, computed, watch, ref } from "vue";
import type { UserResponseDto } from "@/types/backend";

const props = defineProps<{
  visible: boolean;
  user: Partial<UserResponseDto>;
}>();

const emit = defineEmits(["update:visible", "save", "cancel"]);

// State
const saving = ref(false);

const localPasswords = reactive({
  newPassword: "",
  confirmPassword: "",
});

const fieldTouched = reactive({
  newPassword: false,
  confirmPassword: false,
});

// Reset form when user changes
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

// --- NEW SECURITY VALIDATION ---

const isPasswordSecure = computed(() => {
  const password = localPasswords.newPassword;
  return {
    minLength: password.length >= 12,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    digit: /\d/.test(password),
    // Covers a wide range of common special characters
    specialChar: /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password),
  };
});

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

// Validation
const getError = (key: "newPassword" | "confirmPassword") => {
  if (!fieldTouched[key]) return false;

  if (key === "newPassword") {
    // Check if empty OR if security requirements are not met (if not empty)
    return !localPasswords.newPassword || !isNewPasswordSecure.value;
  }

  if (key === "confirmPassword") {
    return (
      !localPasswords.confirmPassword ||
      localPasswords.confirmPassword !== localPasswords.newPassword
    );
  }

  return false;
};

const isFormValid = computed(() => {
  return (
    localPasswords.newPassword &&
    isNewPasswordSecure.value &&
    localPasswords.confirmPassword &&
    localPasswords.newPassword === localPasswords.confirmPassword
  );
});

// --- END NEW SECURITY VALIDATION ---

// Handlers
async function onSave() {
  fieldTouched.newPassword = true;
  fieldTouched.confirmPassword = true;

  if (!isFormValid.value) return;

  saving.value = true;

  try {
    emit("save", { newPassword: localPasswords.newPassword });
  } finally {
    saving.value = false;
  }
}

function onCancel() {
  emit("cancel");
  emit("update:visible", false);
}
</script>
