<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :style="{ width: '550px' }"
    :header="localUser.id ? 'Edit User Details' : 'Create New User'"
    :modal="true"
    class="p-4 bg-surface-0 dark:bg-surface-900 shadow-2xl rounded-xl"
  >
    <!-- Dialog Body using TabView for step-by-step clarity -->
    <TabView
      :activeIndex="activeTabIndex"
      @update:activeIndex="activeTabIndex = $event"
      class="user-dialog-tabview"
    >
      <!-- TAB 1: BASIC DETAILS -->
      <TabPanel
        :value="0"
        :header-class="
          activeTabIndex === 0
            ? 'bg-primary-50 dark:bg-primary-900 rounded-top-lg'
            : ''
        "
      >
        <template #header>
          <div class="flex items-center gap-2">
            <i class="pi pi-user text-lg"></i>
            <span class="font-semibold">Basic Details</span>
          </div>
        </template>

        <div class="flex flex-col gap-5 p-2">
          <!-- Full Name -->
          <div>
            <label for="fullName" class="block font-medium mb-2"
              >Full Name <span class="text-red-500">*</span></label
            >
            <InputText
              id="fullName"
              v-model.trim="localUser.fullName"
              placeholder="Enter full name"
              :invalid="submitted && !localUser.fullName"
              class="w-full"
            />
            <small v-if="submitted && !localUser.fullName" class="text-red-500">
              Full name is required.
            </small>
          </div>

          <!-- Email -->
          <div>
            <label for="email" class="block font-medium mb-2"
              >Email <span class="text-red-500">*</span></label
            >
            <InputText
              id="email"
              type="email"
              v-model.trim="localUser.email"
              placeholder="user@example.com"
              :invalid="
                submitted &&
                (!localUser.email || !isEmailValid(localUser.email))
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
          </div>
        </div>
      </TabPanel>

      <!-- TAB 2: ROLE ASSIGNMENT -->
      <TabPanel
        :value="1"
        :header-class="
          activeTabIndex === 1
            ? 'bg-primary-50 dark:bg-primary-900 rounded-top-lg'
            : ''
        "
      >
        <template #header>
          <div class="flex items-center gap-2">
            <i class="pi pi-briefcase text-lg"></i>
            <span class="font-semibold">Role & Clinic</span>
          </div>
        </template>

        <div class="flex flex-col gap-5 p-2">
          <!-- Role Selection -->
          <div>
            <label for="role" class="block font-medium mb-2"
              >Role <span class="text-red-500">*</span></label
            >
            <Select
              id="role"
              v-model="localUser.roleId"
              :options="roles"
              optionLabel="name"
              optionValue="id"
              placeholder="Select a role"
              :loading="rolesLoading"
              :invalid="submitted && activeTabIndex === 1 && !localUser.roleId"
              class="w-full"
            >
              <template #value="{ value }">
                <div v-if="value" class="flex align-items-center gap-2">
                  <i class="pi pi-shield"></i>
                  <span class="font-semibold">{{
                    roles.find((r) => r.id === value)?.name
                  }}</span>
                </div>
                <span v-else class="text-500">Select a role</span>
              </template>
              <template #option="{ option }">
                <div class="flex flex-col gap-1 p-2">
                  <div class="flex align-items-center gap-2">
                    <i class="pi pi-shield text-primary"></i>
                    <span class="font-semibold">{{ option.name }}</span>
                  </div>
                  <small class="text-500">{{
                    option.description || "No description provided"
                  }}</small>
                </div>
              </template>
            </Select>
            <small
              v-if="submitted && activeTabIndex === 1 && !localUser.roleId"
              class="text-red-500"
            >
              Role is required.
            </small>
          </div>

          <!-- Clinic Selection (SuperAdmin only) -->
          <div v-if="isSuperAdmin">
            <label for="clinic" class="block font-medium mb-2">Clinic</label>
            <Select
              id="clinic"
              v-model="localUser.clinicId"
              :options="clinics"
              optionLabel="name"
              optionValue="id"
              placeholder="Select a clinic (optional)"
              :loading="clinicsLoading"
              showClear
              class="w-full"
              filter
              filterPlaceholder="Search clinics..."
            >
              <template #value="{ value }">
                <div v-if="value" class="flex align-items-center gap-2">
                  <i class="pi pi-building"></i>
                  <span class="font-semibold">{{
                    clinics.find((c) => c.id === value)?.name
                  }}</span>
                </div>
                <span v-else class="text-500">No clinic (Global Admin)</span>
              </template>
              <template #option="{ option }">
                <div class="flex flex-col gap-1 p-2">
                  <div class="flex align-items-center gap-2">
                    <i class="pi pi-building text-green-500"></i>
                    <span class="font-semibold">{{ option.name }}</span>
                  </div>
                  <small class="text-500">{{ option.email }}</small>
                </div>
              </template>
            </Select>
            <small class="text-500 mt-1 block"
              >Leave empty for SuperAdmin without clinic affiliation.</small
            >
          </div>
        </div>
      </TabPanel>

      <!-- TAB 3: SECURITY & STATUS -->
      <TabPanel
        :value="2"
        :header-class="
          activeTabIndex === 2
            ? 'bg-primary-50 dark:bg-primary-900 rounded-top-lg'
            : ''
        "
      >
        <template #header>
          <div class="flex items-center gap-2">
            <i class="pi pi-lock text-lg"></i>
            <span class="font-semibold">Security & Status</span>
          </div>
        </template>

        <div class="flex flex-col gap-5 p-2">
          <!-- Password fields (New User Only) -->
          <template v-if="!localUser.id">
            <div class="p-4 bg-surface-50 dark:bg-surface-800 rounded-xl">
              <h4
                class="text-lg font-bold text-900 mb-3 flex items-center gap-2 dark:text-white"
              >
                Set Initial Password
              </h4>
              <p class="text-sm text-600 dark:text-400 mb-3">
                Password must meet the following security requirements:
              </p>
              <!-- Password Requirements List with dynamic coloring and bold text -->
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
                <!-- Password -->
                <div class="col-12">
                  <label for="password" class="block font-medium mb-2"
                    >Password <span class="text-red-500">*</span></label
                  >
                  <Password
                    id="password"
                    v-model="localUser.password"
                    toggleMask
                    :feedback="true"
                    class="w-full"
                    inputClass="w-full"
                    placeholder="Enter new password"
                    :invalid="
                      submitted &&
                      activeTabIndex === 2 &&
                      (!localUser.password || !isNewPasswordSecure)
                    "
                  />
                  <small
                    v-if="
                      submitted && activeTabIndex === 2 && !localUser.password
                    "
                    class="text-red-500"
                  >
                    Password is required.
                  </small>
                  <small
                    v-else-if="
                      submitted &&
                      activeTabIndex === 2 &&
                      localUser.password &&
                      !isNewPasswordSecure
                    "
                    class="text-red-500"
                  >
                    Password does not meet all security requirements.
                  </small>
                </div>

                <!-- Confirm Password -->
                <div class="col-12">
                  <label for="confirmPassword" class="block font-medium mb-2"
                    >Confirm Password <span class="text-red-500">*</span></label
                  >
                  <Password
                    id="confirmPassword"
                    v-model="localUser.confirmPassword"
                    toggleMask
                    :feedback="false"
                    class="w-full"
                    inputClass="w-full"
                    placeholder="Re-enter password"
                    :invalid="
                      submitted &&
                      activeTabIndex === 2 &&
                      (!localUser.confirmPassword ||
                        localUser.confirmPassword !== localUser.password)
                    "
                  />
                  <small
                    v-if="
                      submitted &&
                      activeTabIndex === 2 &&
                      !localUser.confirmPassword
                    "
                    class="text-red-500"
                  >
                    Confirmation is required.
                  </small>
                  <small
                    v-else-if="
                      submitted &&
                      activeTabIndex === 2 &&
                      localUser.confirmPassword &&
                      localUser.confirmPassword !== localUser.password
                    "
                    class="text-red-500"
                  >
                    Passwords do not match.
                  </small>
                </div>
              </div>
            </div>
          </template>

          <!-- Active Status Toggle -->
          <div
            class="flex items-center justify-between p-4 rounded-lg ring-1 transition-all duration-300"
            :class="{
              'bg-green-50 dark:bg-green-900 ring-green-300 dark:ring-green-600':
                localUser.isActive,
              'bg-red-50 dark:bg-red-900 ring-red-300 dark:ring-red-600':
                !localUser.isActive,
            }"
          >
            <div
              :class="{
                'text-green-900 dark:text-green-100': localUser.isActive,
                'text-red-900 dark:text-red-100': !localUser.isActive,
              }"
            >
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
            <!-- The ToggleSwitch is pushed to the far right by 'justify-between' -->
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
        />

        <div class="flex gap-2">
          <!-- Previous Button -->
          <Button
            v-if="activeTabIndex > 0"
            label="Previous"
            icon="pi pi-arrow-left"
            severity="secondary"
            @click="onPrevious"
          />

          <!-- Next Button (or Save/Update if on last tab) -->
          <Button
            v-if="activeTabIndex < 2"
            label="Next"
            icon="pi pi-arrow-right"
            iconPos="right"
            @click="onNext"
          />

          <!-- Final Save/Update Button -->
          <Button
            v-else
            :label="localUser.id ? 'Update User' : 'Create User'"
            icon="pi pi-check"
            :loading="saving"
            @click="onSave"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted } from "vue";
import { useAuthStore } from "@/stores/authStore";
import { apiService } from "@/service/apiService";
import type {
  RoleResponseDto,
  ClinicResponseDto,
  UserResponseDto,
} from "@/types/backend";

// Internal state for managing the active tab index
const activeTabIndex = ref(0);
// Total number of steps/tabs
const TOTAL_TABS = 3;

const authStore = useAuthStore();

const props = defineProps<{
  visible: boolean;
  user: Partial<UserResponseDto>;
}>();

const emit = defineEmits(["update:visible", "save", "cancel"]);

// State
const localUser = ref<any>({});
const submitted = ref(false);
const saving = ref(false);
const roles = ref<RoleResponseDto[]>([]);
const rolesLoading = ref(false);
const clinics = ref<ClinicResponseDto[]>([]);
const clinicsLoading = ref(false);

const isSuperAdmin = computed(() => authStore.isSuperAdmin);

// Load roles and clinics
onMounted(async () => {
  await Promise.all([
    loadRoles(),
    isSuperAdmin.value ? loadClinics() : Promise.resolve(),
  ]);
});

async function loadRoles() {
  try {
    rolesLoading.value = true;
    roles.value = await apiService.get<RoleResponseDto[]>("/roles");
  } catch (error) {
    console.error("Failed to load roles:", error);
  } finally {
    rolesLoading.value = false;
  }
}

async function loadClinics() {
  try {
    clinicsLoading.value = true;
    clinics.value = await apiService.get<ClinicResponseDto[]>("/clinics");
  } catch (error) {
    console.error("Failed to load clinics:", error);
  } finally {
    clinicsLoading.value = false;
  }
}

// Watch for dialog visibility changes
watch(
  () => props.visible,
  (newVal) => {
    if (newVal) {
      submitted.value = false;
      // Reset to the first tab when opening
      activeTabIndex.value = 0;
      localUser.value = {
        fullName: props.user?.fullName || "",
        email: props.user?.email || "",
        password: "",
        confirmPassword: "",
        roleId: props.user?.roleId || null,
        clinicId: props.user?.clinicId || null,
        isActive: props.user?.isActive ?? true,
        id: props.user?.id || null,
      };
    }
  }
);

// Validation
function isEmailValid(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

// --- NEW PASSWORD SECURITY VALIDATION (for new users) ---

const isPasswordSecure = computed(() => {
  if (localUser.value.id) return {}; // Skip check for existing users

  const password = localUser.value.password || "";
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
  if (localUser.value.id) return true; // Skip check for existing users

  const security = isPasswordSecure.value;
  return (
    security.minLength &&
    security.uppercase &&
    security.lowercase &&
    security.digit &&
    security.specialChar
  );
});

// Separate validation into steps for sequential navigation checks
const isBasicDetailsValid = computed(() => {
  const hasFullName = !!localUser.value.fullName?.trim();
  const hasEmail =
    !!localUser.value.email && isEmailValid(localUser.value.email);
  return hasFullName && hasEmail;
});

const isRoleDetailsValid = computed(() => {
  const hasRole = !!localUser.value.roleId;
  return hasRole;
});

const isSecurityDetailsValid = computed(() => {
  // Password validation only for new users
  if (!localUser.value.id) {
    return (
      isNewPasswordSecure.value &&
      localUser.value.password && // Must not be empty
      localUser.value.password === localUser.value.confirmPassword
    );
  }
  // For existing users, this tab is always valid as it only contains the active status toggle
  return true;
});

const isFormValid = computed(() => {
  return (
    isBasicDetailsValid.value &&
    isRoleDetailsValid.value &&
    isSecurityDetailsValid.value
  );
});

// Navigation Handlers
function onPrevious() {
  if (activeTabIndex.value > 0) {
    activeTabIndex.value--;
  }
}

function onNext() {
  // Set submitted state for the current tab only
  submitted.value = true;

  let isValid = false;
  if (activeTabIndex.value === 0) {
    isValid = isBasicDetailsValid.value;
  } else if (activeTabIndex.value === 1) {
    isValid = isRoleDetailsValid.value;
  }

  if (isValid) {
    submitted.value = false; // Reset submitted state on success to prevent showing errors on the next tab
    activeTabIndex.value++;
  } else {
    // If validation fails, keep submitted state to show errors on the current tab
  }
}

// Handlers
async function onSave() {
  submitted.value = true;
  if (!isFormValid.value) {
    // If validation fails on the final step, jump the user back to the tab with the error
    if (!isBasicDetailsValid.value) {
      activeTabIndex.value = 0;
    } else if (!isRoleDetailsValid.value) {
      activeTabIndex.value = 1;
    } else {
      activeTabIndex.value = 2; // Password error
    }
    return;
  }

  saving.value = true;

  // Prepare DTO for backend
  const dto: any = {
    fullName: localUser.value.fullName,
    email: localUser.value.email,
    roleId: localUser.value.roleId,
    clinicId: localUser.value.clinicId || undefined,
    isActive: localUser.value.isActive, // Include status update for existing user
  };

  // Add password only for new users
  if (!localUser.value.id) {
    dto.password = localUser.value.password;
  }

  try {
    emit("save", { ...dto, id: localUser.value.id });
  } finally {
    saving.value = false;
  }
}

function onCancel() {
  emit("cancel");
  emit("update:visible", false);
}
</script>
