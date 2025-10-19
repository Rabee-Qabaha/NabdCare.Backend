<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Change Password"
    :modal="true"
    :style="{ width: '400px' }"
  >
    <div class="flex flex-col gap-6">
      <div v-for="field in fields" :key="field.key">
        <label :for="field.key" class="block font-bold mb-3">{{
          field.label
        }}</label>
        <Password
          :id="field.key"
          v-model="localPasswords[field.key]"
          toggleMask
          :feedback="false"
          class="w-full"
          inputClass="w-full"
          @input="fieldTouched[field.key] = true"
          :invalid="getError(field.key)"
        />
        <small
          v-if="getError(field.key) && field.key === 'currentPassword'"
          class="text-red-500"
        >
          Current password is required.
        </small>
        <small
          v-if="getError(field.key) && field.key === 'newPassword'"
          class="text-red-500"
        >
          New password is required.
        </small>
        <small
          v-if="
            getError(field.key) &&
            field.key === 'confirmPassword' &&
            !localPasswords.confirmPassword
          "
          class="text-red-500"
        >
          Confirmation is required.
        </small>
        <small
          v-if="
            getError(field.key) &&
            field.key === 'confirmPassword' &&
            localPasswords.confirmPassword !== localPasswords.newPassword
          "
          class="text-red-500"
        >
          Passwords do not match.
        </small>
      </div>
    </div>

    <template #footer>
      <Button label="Cancel" icon="pi pi-times" text @click="onCancel" />
      <Button
        label="Save"
        icon="pi pi-check"
        @click="onSave"
        :disabled="!isFormValid"
        :loading="userStore.loading"
      />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from "vue";
import { useToast } from "primevue/usetoast";
import { useAuthStore } from "@/stores/authStore";
import { useUserStore } from "@/stores/userStore";

const toast = useToast();
const authStore = useAuthStore();
const userStore = useUserStore();

const props = defineProps<{ visible: boolean; user: { uid?: string } }>();
const emit = defineEmits(["update:visible", "save", "cancel"]);

type PasswordField = "currentPassword" | "newPassword" | "confirmPassword";

// Determine user permissions
const isSelf = computed(() => props.user?.uid === authStore.currentUser?.sub);
const isSuperAdmin = computed(() => authStore.isSuperAdmin);
const showCurrentPassword = computed(() => isSelf.value && !isSuperAdmin.value);

// Generate dynamic fields
const fields = computed<{ key: PasswordField; label: string }[]>(() => {
  const base: { key: PasswordField; label: string }[] = [
    { key: "newPassword", label: "New Password" },
    { key: "confirmPassword", label: "Confirm New Password" },
  ];
  if (showCurrentPassword.value) {
    base.unshift({ key: "currentPassword", label: "Current Password" });
  }
  return base;
});

// Local state
const localPasswords: Record<PasswordField, string> = reactive({
  currentPassword: "",
  newPassword: "",
  confirmPassword: "",
});

const fieldTouched: Record<PasswordField, boolean> = reactive({
  currentPassword: false,
  newPassword: false,
  confirmPassword: false,
});

// Reset form when user changes
watch(
  () => props.user,
  () => {
    (Object.keys(localPasswords) as PasswordField[]).forEach(
      (k) => (localPasswords[k] = "")
    );
    (Object.keys(fieldTouched) as PasswordField[]).forEach(
      (k) => (fieldTouched[k] = false)
    );
  },
  { deep: true }
);

// Validation
const getError = (key: PasswordField) => {
  if (!fieldTouched[key]) return false;
  if (key === "currentPassword")
    return showCurrentPassword.value && !localPasswords.currentPassword;
  if (key === "newPassword") return !localPasswords.newPassword;
  if (key === "confirmPassword")
    return (
      !localPasswords.confirmPassword ||
      localPasswords.confirmPassword !== localPasswords.newPassword
    );
  return false;
};

const isFormValid = computed(() => {
  const requireCurrent = showCurrentPassword.value;
  return (
    (!requireCurrent || localPasswords.currentPassword) &&
    localPasswords.newPassword &&
    localPasswords.confirmPassword &&
    localPasswords.newPassword === localPasswords.confirmPassword
  );
});

// Save handler
async function onSave() {
  (Object.keys(fieldTouched) as PasswordField[]).forEach(
    (k) => (fieldTouched[k] = true)
  );
  if (!isFormValid.value || !props.user.uid) return;

  try {
    await userStore.changePassword(props.user.uid, localPasswords.newPassword);
    toast.add({
      severity: "success",
      summary: "Password Changed",
      detail: "Password updated successfully.",
      life: 3000,
    });
    emit("save");
    emit("update:visible", false);
  } catch (error: any) {
    toast.add({
      severity: "error",
      summary: "Error",
      detail: error.message || "Failed to change password",
      life: 3000,
    });
  }
}

// Cancel handler
function onCancel() {
  emit("cancel");
  emit("update:visible", false);
}
</script>
