// src/components/User/ResetPasswordDialog.vue
<script setup lang="ts">
import { ref, watch } from "vue";
import Dialog from "primevue/dialog";
import Button from "primevue/button";
import Avatar from "primevue/avatar";
import Message from "primevue/message";
import UserPasswordFields from "@/components/Forms/UserPasswordFields.vue";
import { useResetPassword } from "@/composables/query/users/useUserActions";
import { useToast } from "primevue/usetoast";

const props = defineProps<{
  visible: boolean;
  userId: string | null;
  fullName?: string;
  email?: string;
}>();

const emit = defineEmits<{
  "update:visible": [boolean];
}>();

const toast = useToast();
const resetMutation = useResetPassword();

const passwordRef = ref<InstanceType<typeof UserPasswordFields> | null>(null);
const passwordState = ref({
  newPassword: "",
  confirmPassword: "",
});

const submitted = ref(false);
const isLoading = ref(false);

watch(
  () => props.visible,
  (open) => {
    if (open) {
      submitted.value = false;
      passwordRef.value?.resetPasswords();
    }
  }
);

async function onSave() {
  submitted.value = true;
  if (!passwordRef.value?.isValid) return;

  isLoading.value = true;
  try {
if (!props.userId) return;

await resetMutation.mutateAsync({
  id: props.userId,
      data: { newPassword: passwordState.value.newPassword },
    });

    toast.add({
      severity: "success",
      summary: "Password Reset",
      detail: "The user password has been successfully reset.",
      life: 2500,
    });

    emit("update:visible", false);
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Reset Password"
    modal
    :style="{ width: '500px' }"
  >
    <!-- User Info -->
    <div class="mb-4 flex items-center gap-3">
      <Avatar :label="fullName?.charAt(0) || '?'" shape="circle" />
      <div>
        <div class="font-semibold">{{ fullName }}</div>
        <div class="text-sm opacity-70">{{ email }}</div>
      </div>
    </div>

    <Message severity="info" :closable="false" class="mb-4">
      As a SuperAdmin, you can reset this user's password without the current password.
    </Message>

    <UserPasswordFields
      ref="passwordRef"
      mode="reset"
      :loading="isLoading"
      :submitted="submitted"
      @update:passwords="passwordState = $event"
    />

    <template #footer>
      <Button
        label="Cancel"
        severity="secondary"
        @click="emit('update:visible', false)"
      />
      <Button
        label="Reset Password"
        icon="pi pi-key"
        severity="warning"
        :loading="isLoading"
        :disabled="!passwordRef?.isValid"
        @click="onSave"
      />
    </template>
  </Dialog>
</template>