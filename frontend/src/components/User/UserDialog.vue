<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { useAuthStore } from "@/stores/authStore";
import {
  useClinics,
  useGroupedRoles,
} from "@/composables/query/useDropdownData";
import type { UserResponseDto } from "@/types/backend";
import { usePasswordValidation } from "@/composables/validation/usePasswordValidation";
import Dialog from "primevue/dialog";
import TabView from "primevue/tabview";
import TabPanel from "primevue/tabpanel";
import InputText from "primevue/inputtext";
import Password from "primevue/password";
import RoleSelect from "@/components/Dropdowns/RoleSelect.vue";
import ClinicSelect from "@/components/Dropdowns/ClinicSelect.vue";
import ToggleSwitch from "primevue/toggleswitch";
import Button from "primevue/button";
import { userApi } from "@/api/modules/users";
import { useRestoreUser } from "@/composables/query/users/useUserActions";
import { useToastService } from "@/composables/useToastService";

const toast = useToastService();
const activeTabIndex = ref(0);
const authStore = useAuthStore();

const props = withDefaults(
  defineProps<{
    visible: boolean;
    user?: Partial<UserResponseDto> | null;
  }>(),
  { user: null }
);

const emit = defineEmits<{
  "update:visible": [value: boolean];
  save: [value: any];
  cancel: [];
}>();

const { mutateAsync: restoreUser } = useRestoreUser();

// Local form state
const localUser = ref<any>({});
const submitted = ref(false);
const saving = ref(false);
const isSuperAdmin = computed(() => authStore.isSuperAdmin);

// ✅ Email conflict states
const emailExistsError = ref(false);
const softDeletedUser = ref<{ userId: string } | null>(null);

// ✅ Restore modal
const showRestoreDialog = ref(false);

async function restoreAccount() {
  if (!softDeletedUser.value) return;

  await restoreUser(softDeletedUser.value.userId); // mutation

  // ✅ Reset UI state
  softDeletedUser.value = null;
  emailExistsError.value = false;
  showRestoreDialog.value = false;

  // ✅ Close the Create User dialog
  emit("update:visible", false);

  toast.success("The user account was successfully restored.");
}

// Password composable
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

// Load dropdowns
const { data: groupedRoles } = useGroupedRoles();
useClinics();

// Computed roles
const allRoles = computed(() => {
  if (!groupedRoles.value) return [];
  const { systemRoles = [], clinicRoles = [] } = groupedRoles.value;
  return [...systemRoles, ...clinicRoles];
});

const selectedRole = computed(() =>
  allRoles.value.find((r) => r.id === localUser.value.roleId)
);

const shouldDisableClinicDropdown = computed(
  () => selectedRole.value?.isSystemRole ?? false
);
const isClinicRequired = computed(() => !selectedRole.value?.isSystemRole);

// Validation
function isEmailValid(email: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

const isBasicDetailsValid = computed(() => {
  return (
    !!localUser.value.fullName?.trim() &&
    !!localUser.value.email &&
    isEmailValid(localUser.value.email)
  );
});

const isRoleDetailsValid = computed(() => !!localUser.value.roleId);

// Password validation (new users only)
const isPasswordValid = computed(() => {
  if (localUser.value.id) return true;
  return (
    passwords.newPassword &&
    passwords.confirmPassword &&
    isNewPasswordSecure.value &&
    doPasswordsMatch.value
  );
});

const isFormValid = computed(
  () =>
    isBasicDetailsValid.value &&
    isRoleDetailsValid.value &&
    (shouldDisableClinicDropdown.value || !!localUser.value.clinicId) &&
    isPasswordValid.value
);

// Watch dialog open
watch(
  () => props.visible,
  (open) => {
    if (open) {
      submitted.value = false;
      activeTabIndex.value = 0;
      emailExistsError.value = false;
      softDeletedUser.value = null;

      localUser.value = {
        fullName: props.user?.fullName || "",
        email: props.user?.email || "",
        roleId: props.user?.roleId || null,
        clinicId: props.user?.clinicId || null,
        isActive: props.user?.isActive ?? true,
        id: props.user?.id || null,
      };

      resetPasswords();
    }
  }
);

// Actions
function onPrevious() {
  if (activeTabIndex.value > 0) activeTabIndex.value--;
}

async function onNext() {
  submitted.value = true;

  // TAB 1: Check email existence
  if (activeTabIndex.value === 0) {
    if (!isBasicDetailsValid.value) return;

    if (!localUser.value.id || localUser.value.email !== props.user?.email) {
      try {
        const { data } = await userApi.CheckEmailExistsDetailed(
          localUser.value.email
        );

        emailExistsError.value = data.exists && !data.isDeleted;
        softDeletedUser.value =
          data.exists && data.isDeleted && data.userId
            ? { userId: data.userId }
            : null;

        if (emailExistsError.value || softDeletedUser.value) return;
      } catch (e) {
        console.error(e);
      }
    }
  }

  activeTabIndex.value++;
}

async function onSave() {
  submitted.value = true;
  if (!isFormValid.value) return;

  saving.value = true;

  const dto: any = {
    fullName: localUser.value.fullName.trim(),
    email: localUser.value.email.trim(),
    roleId: localUser.value.roleId,
    clinicId: localUser.value.clinicId || undefined,
    isActive: localUser.value.isActive,
  };

  if (!localUser.value.id) dto.password = passwords.newPassword;

  emit("save", { ...dto, id: localUser.value.id });
  saving.value = false;
}

function onCancel() {
  emit("cancel");
  emit("update:visible", false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :style="{ width: '550px' }"
    :header="localUser.id ? 'Edit User Details' : 'Create New User'"
    modal
    class="p-4 bg-surface-0 dark:bg-surface-900 shadow-2xl rounded-xl"
  >
    <TabView
      :activeIndex="activeTabIndex"
      @update:activeIndex="activeTabIndex = $event"
    >
      <!-- TAB 1: BASIC DETAILS -->
      <TabPanel>
        <template #header>
          <div class="flex items-center gap-2">
            <i class="pi pi-user text-lg"></i>
            <span class="font-semibold">Basic Details</span>
          </div>
        </template>

        <div class="flex flex-col gap-5 p-2">
          <div>
            <label class="block font-medium mb-2"
              >Full Name <span class="text-red-500">*</span></label
            >
            <InputText
              v-model.trim="localUser.fullName"
              placeholder="Enter full name"
              :invalid="submitted && !localUser.fullName"
              class="w-full"
            />
            <small v-if="submitted && !localUser.fullName" class="text-red-500"
              >Full name is required.</small
            >
          </div>

          <div>
            <label class="block font-medium mb-2">
              Email <span class="text-red-500">*</span>
            </label>

            <InputText
              v-model.trim="localUser.email"
              placeholder="user@example.com"
              type="email"
              :invalid="
                submitted &&
                (!localUser.email ||
                  !isEmailValid(localUser.email) ||
                  emailExistsError)
              "
              class="w-full"
            />

            <small v-if="submitted && !localUser.email" class="text-red-500">
              Email is required.
            </small>
            <small
              v-else-if="
                submitted && localUser.email && !isEmailValid(localUser.email)
              "
              class="text-red-500"
            >
              Please enter a valid email address.
            </small>

            <!-- Normal duplicate -->
            <small v-if="emailExistsError" class="text-red-500">
              This email is already registered. Please use another one.
            </small>

            <!-- Soft-deleted user restore link -->
            <small
              v-if="softDeletedUser"
              class="text-yellow-600 flex items-center gap-2"
            >
              This user existed before and was deleted.
              <button
                class="underline text-primary font-semibold"
                @click="showRestoreDialog = true"
              >
                Restore account?
              </button>
            </small>
          </div>
        </div>
      </TabPanel>

      <!-- TAB 2: ROLE & CLINIC -->
      <TabPanel>
        <template #header>
          <div class="flex items-center gap-2">
            <i class="pi pi-briefcase text-lg"></i>
            <span class="font-semibold">Role & Clinic</span>
          </div>
        </template>

        <div class="flex flex-col gap-5 p-2">
          <div>
            <!-- Role Selection -->
            <RoleSelect
              v-model="localUser.roleId"
              showLabel
              label="Role"
              :invalid="submitted && activeTabIndex === 1 && !localUser.roleId"
              required="true"
            />
            <!-- Validation messages -->
            <small
              v-if="submitted && activeTabIndex === 1 && !localUser.roleId"
              class="text-red-500 block"
            >
              Role is required.
            </small>
          </div>

          <div>
            <!-- Clinic Selection -->
            <ClinicSelect
              v-if="isSuperAdmin"
              v-model="localUser.clinicId"
              showLabel
              label="Clinic"
              :disabled="shouldDisableClinicDropdown"
              :invalid="
                submitted &&
                activeTabIndex === 1 &&
                isClinicRequired &&
                !localUser.clinicId
              "
              required="true"
            />
            <small
              v-if="
                submitted &&
                activeTabIndex === 1 &&
                isClinicRequired &&
                !localUser.clinicId
              "
              class="text-red-500 block"
            >
              Clinic is required for this role.
            </small>
          </div>
        </div>
      </TabPanel>

      <!-- TAB 3: SECURITY -->
      <TabPanel>
        <template #header>
          <div class="flex items-center gap-2">
            <i class="pi pi-lock text-lg"></i>
            <span class="font-semibold">Security & Status</span>
          </div>
        </template>

        <!-- Password Fields -->
        <div class="flex flex-col gap-4">
          <!-- ✅ Only show password fields for new users -->
          <div
            v-if="!localUser.id"
            class="p-4 bg-surface-50 dark:bg-surface-800 rounded-lg"
          >
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
              <div>
                <label
                  for="newPassword"
                  class="block font-semibold mb-2 dark:text-white"
                  >New Password <span class="text-red-500">*</span></label
                >
                <Password
                  id="newPassword"
                  v-model="passwords.newPassword"
                  toggleMask
                  :feedback="true"
                  class="w-full"
                  inputClass="w-full"
                  placeholder="Enter new password"
                  @input="markFieldTouched('newPassword')"
                  :invalid="submitted && getFieldError('newPassword')"
                  :disabled="saving"
                />
                <small
                  v-if="submitted && getFieldError('newPassword')"
                  class="text-red-500"
                >
                  {{ getFieldErrorMessage("newPassword") }}
                </small>
              </div>

              <!-- Confirm Password Input -->
              <div>
                <label
                  for="confirmPassword"
                  class="block font-semibold mb-2 dark:text-white"
                  >Confirm New Password
                  <span class="text-red-500">*</span></label
                >
                <Password
                  id="confirmPassword"
                  v-model="passwords.confirmPassword"
                  toggleMask
                  :feedback="false"
                  class="w-full"
                  inputClass="w-full"
                  placeholder="Re-enter new password"
                  @input="markFieldTouched('confirmPassword')"
                  :invalid="submitted && getFieldError('confirmPassword')"
                  :disabled="saving"
                />
                <small
                  v-if="submitted && getFieldError('confirmPassword')"
                  class="text-red-500"
                >
                  {{ getFieldErrorMessage("confirmPassword") }}
                </small>
              </div>
            </div>
          </div>

          <!-- ✅ Message for existing users -->
          <div v-else class="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <div class="flex items-start gap-3">
              <i
                class="pi pi-info-circle text-blue-600 dark:text-blue-400 text-lg mt-0.5"
              ></i>
              <div>
                <p
                  class="text-sm font-semibold text-blue-900 dark:text-blue-100 m-0"
                >
                  Password Management
                </p>
                <p class="text-sm text-blue-800 dark:text-blue-200 m-0 mt-1">
                  Use the dedicated password reset feature to change this user's
                  password.
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Active Status Toggle -->
        <div class="flex flex-col gap-5 p-2 mt-4">
          <div
            class="flex items-center justify-between p-4 rounded-lg ring-1 transition-all duration-300"
            :class="{
              'bg-green-50 dark:bg-green-900 ring-green-300 dark:ring-green-600':
                localUser.isActive,
              'bg-red-50 dark:bg-red-900 ring-red-300 dark:ring-red-600':
                !localUser.isActive,
            }"
          >
            <div>
              <label class="font-bold text-lg flex items-center gap-2">
                <i
                  :class="{
                    'pi pi-check-circle text-xl': localUser.isActive,
                    'pi pi-ban text-xl': !localUser.isActive,
                  }"
                ></i>
                Active Status
              </label>
              <p class="text-sm m-0">
                {{
                  localUser.isActive
                    ? "User account is enabled and ready for login."
                    : "User account is disabled and login is blocked."
                }}
              </p>
            </div>
            <div class="flex-shrink-0">
              <ToggleSwitch v-model="localUser.isActive" />
            </div>
          </div>
        </div>
      </TabPanel>
    </TabView>

    <template #footer>
      <div class="flex justify-between gap-2 p-2">
        <Button
          label="Cancel"
          icon="pi pi-times"
          severity="secondary"
          outlined
          @click="onCancel"
          :disabled="saving"
        />
        <div class="flex gap-2">
          <Button
            v-if="activeTabIndex > 0"
            label="Previous"
            icon="pi pi-arrow-left"
            severity="secondary"
            @click="onPrevious"
            :disabled="saving"
          />
          <Button
            v-if="activeTabIndex < 2"
            label="Next"
            icon="pi pi-arrow-right"
            iconPos="right"
            @click="onNext"
            :disabled="saving"
          />
          <Button
            v-else
            :label="localUser.id ? 'Update User' : 'Create User'"
            icon="pi pi-check"
            :loading="saving"
            :disabled="!isFormValid || saving"
            @click="onSave"
          />
        </div>
      </div>
    </template>
  </Dialog>

  <Dialog
    v-model:visible="showRestoreDialog"
    header="Restore User Account"
    modal
  >
    <p class="mb-4">
      This user account was previously deleted. Do you want to restore it?
    </p>

    <div class="flex justify-end gap-2">
      <Button
        label="Cancel"
        severity="secondary"
        @click="showRestoreDialog = false"
      />
      <Button
        label="Restore"
        icon="pi pi-undo"
        severity="success"
        @click="restoreAccount"
      />
    </div>
  </Dialog>
</template>
