// src/components/User/UserDialog.vue
<template>
  <Dialog
    v-model:visible="visible"
    :modal="true"
    :closable="!saving"
    :style="{ width: '700px', maxWidth: '95vw' }"
    :pt="{
      root: { class: 'rounded-xl border-0 shadow-2xl overflow-hidden' },
      header: {
        class:
          'border-b border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-white dark:bg-surface-900',
      },
      content: { class: 'p-0 bg-white dark:bg-surface-900' },
      footer: {
        class:
          'border-t border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-surface-50 dark:bg-surface-800',
      },
    }"
    @hide="onClose"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div
          class="flex items-center justify-center w-10 h-10 rounded-full bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 shrink-0"
        >
          <i v-if="!localUser.id" class="pi pi-user-plus text-xl"></i>
          <i v-else class="pi pi-user-edit text-xl"></i>
        </div>
        <div class="flex flex-col">
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight">
            {{ localUser.id ? 'Edit User' : 'Create New User' }}
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400">
            {{ localUser.id ? 'Update user details and access.' : 'Add a new user to the system.' }}
          </p>
        </div>
      </div>
    </template>

    <div class="p-6 flex flex-col gap-6 overflow-y-auto custom-scrollbar" style="max-height: 650px">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
            Full Name
            <span class="text-red-500">*</span>
          </label>
          <InputText
            v-model="fullName"
            placeholder="e.g. John Doe"
            :invalid="submitted && !isFullNameValid"
            class="w-full"
          />
          <small v-if="submitted && !isFullNameValid" class="text-red-500 text-xs">
            Name is required.
          </small>
        </div>

        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
            Email Address
            <span class="text-red-500">*</span>
          </label>
          <InputText
            v-model="email"
            type="email"
            placeholder="user@example.com"
            :invalid="submitted && (!isEmailValid || emailExistsError)"
            class="w-full"
            @blur="checkEmail"
          />
          <small v-if="emailExistsError" class="text-red-500 text-xs">Email already exists.</small>
          <small v-if="softDeletedUser" class="text-orange-500 text-xs flex items-center gap-1">
            User is in trash.
            <span class="underline cursor-pointer font-bold" @click="showRestoreDialog = true">
              Restore?
            </span>
          </small>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
            Role
            <span class="text-red-500">*</span>
          </label>
          <RoleSelect v-model="roleId" :invalid="submitted && !isRoleSelected" class="w-full" />
          <small v-if="submitted && !isRoleSelected" class="text-red-500 text-xs">
            Role is required.
          </small>
        </div>

        <div v-if="isSuperAdmin" class="flex flex-col gap-1.5">
          <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">Clinic</label>
          <ClinicSelect
            v-model="clinicId"
            :disabled="shouldDisableClinic"
            class="w-full"
            :invalid="submitted && isClinicRequired && !isClinicSelected"
            placeholder="Select Clinic"
          />
          <small v-if="shouldDisableClinic" class="text-xs text-surface-500 dark:text-surface-400">
            System roles are global.
          </small>
        </div>

        <div v-else class="flex flex-col gap-1.5">
          <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">Clinic</label>
          <div
            class="h-[42px] flex items-center px-3 rounded-md border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-800 text-surface-500 dark:text-surface-400 text-sm italic"
          >
            Current Clinic Only
          </div>
        </div>
      </div>

      <Divider class="my-0 border-surface-200 dark:border-surface-700" />

      <div
        v-if="!localUser.id"
        class="bg-surface-50 dark:bg-surface-800/50 p-4 rounded-lg border border-surface-200 dark:border-surface-700"
      >
        <h4 class="text-xs font-bold text-surface-500 dark:text-surface-400 uppercase mb-3">
          Security Setup
        </h4>
        <UserPasswordFields
          ref="passwordRef"
          mode="create"
          :submitted="submitted"
          @update:passwords="Object.assign(passwords, $event)"
        />
      </div>

      <div
        class="flex items-center justify-between p-4 rounded-lg border transition-colors"
        :class="
          isActive
            ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
            : 'bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-800'
        "
      >
        <div class="flex flex-col">
          <span
            class="text-sm font-bold"
            :class="
              isActive
                ? 'text-green-700 dark:text-green-300'
                : 'text-orange-700 dark:text-orange-300'
            "
          >
            {{ isActive ? 'Active Account' : 'Inactive Account' }}
          </span>
          <span
            class="text-xs opacity-80"
            :class="
              isActive
                ? 'text-green-600 dark:text-green-400'
                : 'text-orange-600 dark:text-orange-400'
            "
          >
            {{
              isActive
                ? 'User can log in and access the system.'
                : 'User access is temporarily suspended.'
            }}
          </span>
        </div>
        <ToggleSwitch v-model="isActive" />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 w-full mt-4">
        <Button
          label="Cancel"
          severity="secondary"
          outlined
          class="w-full sm:w-auto"
          :disabled="saving"
          @click="onClose"
        />
        <Button
          :label="localUser.id ? 'Save Changes' : 'Create User'"
          :loading="saving"
          icon="pi pi-check"
          class="w-full sm:w-auto"
          @click="onSave"
        />
      </div>
    </template>
  </Dialog>

  <Dialog
    v-model:visible="showRestoreDialog"
    header="Restore User?"
    modal
    :style="{ width: '400px' }"
  >
    <p class="mb-4 text-surface-700 dark:text-surface-200">
      This email belongs to a deleted user. Do you want to restore their account instead of creating
      a new one?
    </p>
    <template #footer>
      <div class="flex justify-end gap-2">
        <Button label="Cancel" text severity="secondary" @click="showRestoreDialog = false" />
        <Button label="Restore User" severity="success" icon="pi pi-undo" @click="restoreAccount" />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import Button from 'primevue/button';
  import Dialog from 'primevue/dialog';
  import Divider from 'primevue/divider';
  import InputText from 'primevue/inputtext';
  import ToggleSwitch from 'primevue/toggleswitch';

  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';
  import RoleSelect from '@/components/Dropdowns/RoleSelect.vue';
  import UserPasswordFields from '@/components/Forms/UserPasswordFields.vue';

  import { usersApi } from '@/api/modules/users';
  import { useGroupedRoles } from '@/composables/query/useDropdownData';
  import { useUserActions } from '@/composables/query/users/useUserActions';
  import { useUserForm } from '@/composables/user/useUserForm';
  import { useToastService } from '@/composables/useToastService';
  import { usePasswordValidation } from '@/composables/validation/usePasswordValidation';
  import { useAuthStore } from '@/stores/authStore';
  import type { UserResponseDto } from '@/types/backend';
  import { computed, ref, watch } from 'vue';

  // Props
  const props = withDefaults(
    defineProps<{
      visible: boolean;
      user?: Partial<UserResponseDto> | null;
    }>(),
    { user: null },
  );

  const emit = defineEmits<{ 'update:visible': [boolean]; save: [any] }>();

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  // Stores & Logic
  const toast = useToastService();
  const authStore = useAuthStore();
  const isSuperAdmin = computed(() => authStore.isSuperAdmin);

  const { restoreMutation } = useUserActions();
  const { data: groupedRoles } = useGroupedRoles();

  // Form State
  const submitted = ref(false);
  const saving = ref(false);
  const emailExistsError = ref(false);
  const softDeletedUser = ref<{ userId: string } | null>(null);
  const showRestoreDialog = ref(false);

  const localUser = ref<Partial<UserResponseDto>>({});
  const {
    fullName,
    email,
    roleId,
    clinicId,
    isActive,
    isFullNameValid,
    isEmailValid,
    isRoleSelected,
    isClinicSelected,
  } = useUserForm(localUser);

  const { passwords } = usePasswordValidation();
  const passwordRef = ref<InstanceType<typeof UserPasswordFields> | null>(null);

  // Derived
  const allRoles = computed(() =>
    groupedRoles.value
      ? [...(groupedRoles.value.systemRoles || []), ...(groupedRoles.value.clinicRoles || [])]
      : [],
  );
  const selectedRole = computed(() => allRoles.value.find((r) => r.id === roleId.value));
  const shouldDisableClinic = computed(() => selectedRole.value?.isSystemRole ?? false);
  const isClinicRequired = computed(() => !selectedRole.value?.isSystemRole);

  const isFormValid = computed(() => {
    const baseValid =
      isFullNameValid.value &&
      isEmailValid.value &&
      isRoleSelected.value &&
      !emailExistsError.value;
    const clinicValid = shouldDisableClinic.value || isClinicSelected.value;
    const passwordValid = localUser.value.id ? true : passwordRef.value?.isValid;
    return baseValid && clinicValid && passwordValid;
  });

  // Watchers
  watch(
    () => props.visible,
    (open) => {
      if (open) {
        submitted.value = false;
        emailExistsError.value = false;
        softDeletedUser.value = null;

        localUser.value = {
          id: props.user?.id,
          fullName: props.user?.fullName || '',
          email: props.user?.email || '',
          roleId: props.user?.roleId,
          clinicId: props.user?.clinicId,
          isActive: props.user?.isActive ?? true,
        };

        if (!localUser.value.id) {
          passwordRef.value?.resetPasswords();
        }
      }
    },
  );

  // Actions
  async function checkEmail() {
    if (!email.value || !isEmailValid.value) return;
    if (localUser.value.id && email.value === props.user?.email) return;

    try {
      const { data } = await usersApi.checkEmailExists(email.value);
      emailExistsError.value = data.exists && !data.isDeleted;
      softDeletedUser.value =
        data.exists && data.isDeleted && typeof data.userId === 'string'
          ? { userId: data.userId }
          : null;
    } catch {
      // ignore error
    }
  }

  async function onSave() {
    submitted.value = true;
    if (!isFormValid.value) {
      toast.error('Please fix the errors before saving.');
      return;
    }

    saving.value = true;
    const dto: any = {
      fullName: fullName.value.trim(),
      email: email.value.trim(),
      roleId: roleId.value,
      clinicId: clinicId.value || undefined,
      isActive: isActive.value,
    };

    if (!localUser.value.id) {
      dto.password = passwords.newPassword;
    }

    emit('save', { ...dto, id: localUser.value.id });
    saving.value = false;
  }

  async function restoreAccount() {
    if (!softDeletedUser.value) return;
    await restoreMutation.mutateAsync(softDeletedUser.value.userId);
    // toast.success('User restored successfully.');
    showRestoreDialog.value = false;
    visible.value = false;
    emit('save', { restored: true });
  }

  function onClose() {
    if (!saving.value) visible.value = false;
  }
</script>

<style scoped>
  .custom-scrollbar::-webkit-scrollbar {
    width: 6px;
  }
  .custom-scrollbar::-webkit-scrollbar-track {
    background: transparent;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb {
    background-color: var(--surface-300);
    border-radius: 20px;
  }
  .custom-scrollbar::-webkit-scrollbar-thumb:hover {
    background-color: var(--surface-400);
  }
</style>
