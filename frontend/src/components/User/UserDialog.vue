<!-- src/components/User/UserDialog.vue -->
<script setup lang="ts">
  import Dialog from 'primevue/dialog';
  import Step from 'primevue/step';
  import StepList from 'primevue/steplist';
  import StepPanel from 'primevue/steppanel';
  import StepPanels from 'primevue/steppanels';
  import Stepper from 'primevue/stepper';
  import { computed, ref, watch } from 'vue';

  import Button from 'primevue/button';
  import FloatLabel from 'primevue/floatlabel';
  import InputText from 'primevue/inputtext';
  import ToggleSwitch from 'primevue/toggleswitch';

  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';
  import RoleSelect from '@/components/Dropdowns/RoleSelect.vue';
  import UserPasswordFields from '@/components/Forms/UserPasswordFields.vue';

  import { usersApi } from '@/api/modules/users';
  import { useClinics, useGroupedRoles } from '@/composables/query/useDropdownData';
  import { useRestoreUser } from '@/composables/query/users/useUserActions';
  import { useUserForm } from '@/composables/user/useUserForm';
  import { useToastService } from '@/composables/useToastService';
  import { usePasswordValidation } from '@/composables/validation/usePasswordValidation';
  import { useAuthStore } from '@/stores/authStore';

  import type { UserResponseDto } from '@/types/backend';

  // Props
  const props = withDefaults(
    defineProps<{
      visible: boolean;
      user?: Partial<UserResponseDto> | null;
    }>(),
    { user: null },
  );

  const emit = defineEmits<{
    'update:visible': [boolean];
    save: [any];
  }>();

  const visible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
  });

  // Stores & services
  const toast = useToastService();
  const authStore = useAuthStore();

  // Stepper state
  const activeStep = ref('1');
  const submitted = ref(false);
  const saving = ref(false);

  // Email validation state
  const emailExistsError = ref(false);
  const softDeletedUser = ref<{ userId: string } | null>(null);
  const showRestoreDialog = ref(false);

  const isSuperAdmin = computed(() => authStore.isSuperAdmin);

  // Queries
  useClinics();
  const { data: groupedRoles } = useGroupedRoles();
  const { mutateAsync: restoreUser } = useRestoreUser();

  // Roles
  const allRoles = computed(() => {
    if (!groupedRoles.value) return [];
    return [...(groupedRoles.value.systemRoles || []), ...(groupedRoles.value.clinicRoles || [])];
  });

  // User model
  const localUser = ref<Partial<UserResponseDto>>({});

  // Form composable
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

  // Password composable (only stores the password values)
  const { passwords, resetPasswords } = usePasswordValidation();

  // Use ref to validate like ResetPasswordDialog
  const passwordRef = ref<InstanceType<typeof UserPasswordFields> | null>(null);

  // Role rules
  const selectedRole = computed(() => allRoles.value.find((r) => r.id === roleId.value));
  const shouldDisableClinic = computed(() => selectedRole.value?.isSystemRole ?? false);
  const isClinicRequired = computed(() => !selectedRole.value?.isSystemRole);

  // Final validation
  const isFormValid = computed(() => {
    const step1Valid = isFullNameValid.value && isEmailValid.value && !emailExistsError.value;
    const step2Valid =
      isRoleSelected.value && (shouldDisableClinic.value || isClinicSelected.value);

    const step3Valid = localUser.value.id ? true : passwordRef.value?.isValid;

    return step1Valid && step2Valid && step3Valid;
  });

  // Reset dialog on open
  watch(
    () => props.visible,
    (open) => {
      if (!open) return;

      submitted.value = false;
      activeStep.value = '1';
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

      passwordRef.value?.resetPasswords();
    },
  );

  // Step 1 handler
  async function handleStep1Next(activateCallback: (step: string) => void) {
    submitted.value = true;

    if (!isFullNameValid.value || !isEmailValid.value) return;

    if (!localUser.value.id || localUser.value.email !== props.user?.email) {
      const { data } = await usersApi.checkEmailExists(localUser.value.email!);

      emailExistsError.value = data.exists && !data.isDeleted;

      softDeletedUser.value =
        data.exists && data.isDeleted && typeof data.userId === 'string'
          ? { userId: data.userId }
          : null;

      if (emailExistsError.value || softDeletedUser.value) return;
    }

    activateCallback('2');
  }

  // Save user
  async function onSave() {
    submitted.value = true;

    if (!isFormValid.value) return;

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

  // Restore soft deleted user
  async function restoreAccount() {
    if (!softDeletedUser.value) return;
    await restoreUser(softDeletedUser.value.userId);

    toast.success('User restored successfully.');

    softDeletedUser.value = null;
    emailExistsError.value = false;
    showRestoreDialog.value = false;
    visible.value = false;
  }
</script>

<template>
  <Dialog
    v-model:visible="visible"
    :style="{ width: '550px' }"
    :header="localUser.id ? 'Edit User' : 'Create User'"
    modal
    class="rounded-xl p-4"
  >
    <Stepper v-model:value="activeStep">
      <StepList>
        <Step value="1"><span class="text-xs font-medium">Basic Details</span></Step>
        <Step value="2"><span class="text-xs font-medium">Role & Clinic</span></Step>
        <Step value="3"><span class="text-xs font-medium">Security & Status</span></Step>
      </StepList>

      <StepPanels>
        <!-- STEP 1 -->
        <StepPanel value="1" v-slot="{ activateCallback }">
          <div class="flex flex-col gap-6 p-3">
            <FloatLabel variant="on">
              <InputText
                v-model="fullName"
                id="fullName"
                class="w-full"
                :invalid="submitted && !isFullNameValid"
              />
              <label for="fullName">Full Name *</label>
            </FloatLabel>

            <FloatLabel variant="on">
              <InputText
                v-model="email"
                id="email"
                type="email"
                class="w-full"
                :invalid="submitted && (!isEmailValid || emailExistsError)"
              />
              <label for="email">Email *</label>
            </FloatLabel>

            <small v-if="emailExistsError" class="text-red-500">This email already exists.</small>

            <small v-if="softDeletedUser" class="text-yellow-600">
              This email belongs to a deleted user.
              <button class="text-primary underline" @click="showRestoreDialog = true">
                Restore?
              </button>
            </small>
          </div>

          <div class="flex justify-between pt-6">
            <Button
              label="Cancel"
              outlined
              severity="secondary"
              icon="pi pi-times"
              @click="visible = false"
            />
            <Button
              label="Next"
              icon="pi pi-arrow-right"
              @click="handleStep1Next(activateCallback)"
            />
          </div>
        </StepPanel>

        <!-- STEP 2 -->
        <StepPanel value="2" v-slot="{ activateCallback }">
          <div class="flex flex-col gap-6 p-3">
            <RoleSelect
              v-model="roleId"
              label="Select a Role"
              showLabel
              required
              :showClear="true"
              :invalid="submitted && !isRoleSelected"
            />

            <ClinicSelect
              v-if="isSuperAdmin"
              v-model="clinicId"
              label="Select a Clinic"
              showLabel
              required
              :showClear="true"
              :disabled="shouldDisableClinic"
              :invalid="submitted && isClinicRequired && !isClinicSelected"
            />
          </div>

          <div class="flex justify-between pt-6">
            <Button
              label="Cancel"
              outlined
              severity="secondary"
              icon="pi pi-times"
              @click="visible = false"
            />

            <div class="flex gap-2">
              <Button
                label="Previous"
                icon="pi pi-arrow-left"
                severity="secondary"
                @click="activateCallback('1')"
              />
              <Button label="Next" icon="pi pi-arrow-right" @click="activateCallback('3')" />
            </div>
          </div>
        </StepPanel>

        <!-- STEP 3 -->
        <StepPanel value="3" v-slot="{ activateCallback }">
          <div class="flex flex-col gap-6 p-3">
            <!-- MATCH ResetPasswordDialog -->
            <UserPasswordFields
              ref="passwordRef"
              mode="create"
              :submitted="submitted"
              :loading="saving"
              @update:passwords="Object.assign(passwords, $event)"
            />

            <div
              class="p-4 rounded-lg ring-1"
              :class="isActive ? 'bg-green-50 ring-green-300' : 'bg-red-50 ring-red-300'"
            >
              <div class="flex justify-between items-center">
                <div>
                  <label class="font-bold flex items-center gap-2">
                    <i :class="isActive ? 'pi pi-check-circle' : 'pi pi-ban'"></i>
                    Status
                  </label>
                  <p class="text-sm">
                    {{ isActive ? 'User is active.' : 'User is inactive.' }}
                  </p>
                </div>

                <ToggleSwitch v-model="isActive" />
              </div>
            </div>
          </div>

          <div class="flex justify-between pt-6">
            <Button
              label="Cancel"
              outlined
              severity="secondary"
              icon="pi pi-times"
              @click="visible = false"
            />

            <div class="flex gap-2">
              <Button
                label="Previous"
                icon="pi pi-arrow-left"
                severity="secondary"
                @click="activateCallback('2')"
              />

              <Button
                :label="localUser.id ? 'Update User' : 'Create User'"
                icon="pi pi-check"
                :disabled="!isFormValid || saving"
                :loading="saving"
                @click="onSave"
              />
            </div>
          </div>
        </StepPanel>
      </StepPanels>
    </Stepper>
  </Dialog>

  <!-- Restore Dialog -->
  <Dialog v-model:visible="showRestoreDialog" header="Restore Account" modal>
    <p>This user was deleted. Restore now?</p>

    <template #footer>
      <Button label="Cancel" severity="secondary" @click="showRestoreDialog = false" />
      <Button label="Restore" icon="pi pi-undo" severity="success" @click="restoreAccount" />
    </template>
  </Dialog>
</template>
