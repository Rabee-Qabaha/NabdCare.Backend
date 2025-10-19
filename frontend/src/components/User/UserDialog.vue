<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :style="{ width: '400px' }"
    :header="localUser.uid ? 'Edit User' : 'Add User'"
    :modal="true"
  >
    <div class="flex flex-col gap-6">
      <!-- Username -->
      <div>
        <label for="username" class="block font-bold mb-3">Username</label>
        <InputText
          id="username"
          v-model.trim="localUser.displayName"
          required
          :invalid="submitted && !localUser.displayName"
          fluid
        />
        <small v-if="submitted && !localUser.displayName" class="text-red-500">
          Username is required.
        </small>
      </div>

      <!-- Email -->
      <div>
        <label for="email" class="block font-bold mb-3">Email</label>
        <InputText
          id="email"
          type="email"
          v-model.trim="localUser.email"
          required
          :invalid="
            submitted && (!localUser.email || !isEmailValid(localUser.email))
          "
          fluid
        />
        <small v-if="submitted && !localUser.email" class="text-red-500">
          Email is required.
        </small>
        <small
          v-if="submitted && localUser.email && !isEmailValid(localUser.email)"
          class="text-red-500"
        >
          Email is invalid.
        </small>
      </div>

      <!-- Passwords -->
      <template v-if="!localUser.uid">
        <div>
          <label for="password" class="block font-bold mb-3">Password</label>
          <Password
            id="password"
            v-model="localUser.password"
            toggleMask
            :feedback="false"
            class="w-full"
            inputClass="w-full"
            required
            :invalid="submitted && !localUser.password"
          />
          <small v-if="submitted && !localUser.password" class="text-red-500">
            Password is required.
          </small>
        </div>

        <div>
          <label for="passwordConfirmation" class="block font-bold mb-3"
            >Confirm Password</label
          >
          <Password
            id="passwordConfirmation"
            v-model="localUser.passwordConfirmation"
            toggleMask
            :feedback="false"
            class="w-full"
            inputClass="w-full"
            required
            :invalid="
              submitted &&
              (!localUser.passwordConfirmation ||
                localUser.passwordConfirmation !== localUser.password)
            "
          />
          <small
            v-if="submitted && !localUser.passwordConfirmation"
            class="text-red-500"
          >
            Password confirmation is required.
          </small>
          <small
            v-if="
              submitted &&
              localUser.passwordConfirmation &&
              localUser.passwordConfirmation !== localUser.password
            "
            class="text-red-500"
          >
            Passwords do not match.
          </small>
        </div>
      </template>

      <!-- Role -->
      <div>
        <label for="role" class="block font-bold mb-3">Role</label>
        <Select
          id="role"
          v-model="localUser.role"
          :options="roleOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Select Role"
          :invalid="submitted && !localUser.role"
          fluid
        />
        <small v-if="submitted && !localUser.role" class="text-red-500"
          >Role is required.</small
        >
      </div>

      <!-- Status -->
      <div>
        <label for="status" class="block font-bold mb-3">Status</label>
        <ToggleButton
          v-model="localUser.isActive"
          onLabel="Active"
          offLabel="Inactive"
          onIcon="pi pi-check"
          offIcon="pi pi-times-circle"
          class="w-full"
        />
      </div>
    </div>

    <template #footer>
      <Button label="Cancel" icon="pi pi-times" text @click="onCancel" />
      <!-- <Button label="Save" icon="pi pi-check" :loading="userStore.isProcessing" @click="onSave" /> -->
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue";
// import type { UserForm } from '@/../../shared/types';
// import { useUserStore } from '@/stores/userStore';

// const userStore = useUserStore();

const roleOptions = [
  { label: "Admin", value: "admin" },
  { label: "User", value: "user" },
];

const props = defineProps<{
  visible: boolean;
  user: Partial<any>; // Replace 'any' with 'UserForm' when the type is available
}>();

const emit = defineEmits(["update:visible", "save", "cancel"]);

const localUser = ref<Partial<any>>({
  // Replace 'any' with 'UserForm' when the type is available
  ...props.user,
  password: "",
  passwordConfirmation: "",
  isActive: props.user?.isActive ?? true,
});

const submitted = ref(false);

watch(
  () => props.visible,
  (newVal) => {
    if (newVal) {
      submitted.value = false;
      localUser.value = {
        ...props.user,
        password: "",
        passwordConfirmation: "",
        isActive: props.user?.isActive ?? true,
      };
    }
  }
);

function isEmailValid(email: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

const isFormValid = computed(() => {
  const hasDisplayName = !!localUser.value.displayName?.trim();
  const hasEmail =
    !!localUser.value.email && isEmailValid(localUser.value.email);
  const hasRole = !!localUser.value.role;
  const passwordValid =
    localUser.value.uid ||
    (localUser.value.password &&
      localUser.value.passwordConfirmation === localUser.value.password);

  return hasDisplayName && hasEmail && hasRole && passwordValid;
});

function onSave() {
  submitted.value = true;
  if (!isFormValid.value) return;

  emit("save", { ...localUser.value });
}

function onCancel() {
  emit("cancel");
  emit("update:visible", false);
}
</script>
