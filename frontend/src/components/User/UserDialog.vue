<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { useAuthStore } from "@/stores/authStore";
import {
  useClinics,
  useGroupedRoles,
} from "@/composables/query/useDropdownData";
import type { UserResponseDto } from "@/types/backend";

// PrimeVue Components
import Dialog from "primevue/dialog";
import TabView from "primevue/tabview";
import TabPanel from "primevue/tabpanel";
import InputText from "primevue/inputtext";
import Select from "primevue/select";
import ToggleSwitch from "primevue/toggleswitch";
import Button from "primevue/button";
import Tag from "primevue/tag";

/**
 * User Dialog Component
 * Author: Rabee Qabaha
 * Updated: 2025-11-02
 */

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

// =========================================================
// STATE
// =========================================================
const localUser = ref<any>({});
const submitted = ref(false);
const saving = ref(false);
const isSuperAdmin = computed(() => authStore.isSuperAdmin);

// =========================================================
// DATA COMPOSABLES
// =========================================================
const {
  data: groupedRoles,
  isLoading: rolesLoading,
  error: rolesError,
} = useGroupedRoles();

const {
  data: clinics = [],
  isLoading: clinicsLoading,
  error: clinicsError,
} = useClinics();

// =========================================================
// ROLE LOGIC
// =========================================================
const allRoles = computed(() => {
  if (!groupedRoles.value) return [];
  const {
    systemRoles = [],
    templateRoles = [],
    clinicRoles = [],
  } = groupedRoles.value;
  return [...systemRoles, ...templateRoles, ...clinicRoles];
});

const selectedRole = computed(() =>
  allRoles.value.find((r) => r.id === localUser.value.roleId)
);

const shouldDisableClinicDropdown = computed(() => {
  if (!selectedRole.value) return false;
  return (
    selectedRole.value.isSystemRole ||
    selectedRole.value.isTemplate ||
    !!selectedRole.value.clinicId
  );
});

const isClinicRequired = computed(() => {
  return (
    !selectedRole.value?.isSystemRole &&
    !selectedRole.value?.isTemplate &&
    !selectedRole.value?.clinicId
  );
});

// =========================================================
// VALIDATION HELPERS
// =========================================================
function isEmailValid(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

const isPasswordSecure = computed(() => {
  if (localUser.value.id) return {};
  const password = localUser.value.password || "";
  return {
    minLength: password.length >= 12,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    digit: /\d/.test(password),
    specialChar: /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password),
  };
});

const isNewPasswordSecure = computed(() => {
  if (localUser.value.id) return true;
  const s = isPasswordSecure.value;
  return s.minLength && s.uppercase && s.lowercase && s.digit && s.specialChar;
});

const isBasicDetailsValid = computed(() => {
  const hasFullName = !!localUser.value.fullName?.trim();
  const hasEmail =
    !!localUser.value.email && isEmailValid(localUser.value.email);
  return hasFullName && hasEmail;
});

const isRoleDetailsValid = computed(() => !!localUser.value.roleId);

const isSecurityDetailsValid = computed(() => {
  if (!localUser.value.id) {
    return (
      isNewPasswordSecure.value &&
      !!localUser.value.password &&
      localUser.value.password === localUser.value.confirmPassword
    );
  }
  return true;
});

const isFormValid = computed(
  () =>
    isBasicDetailsValid.value &&
    isRoleDetailsValid.value &&
    isSecurityDetailsValid.value &&
    (shouldDisableClinicDropdown.value || !!localUser.value.clinicId)
);

// =========================================================
// WATCHERS
// =========================================================
watch(
  () => props.visible,
  (newVal) => {
    if (newVal) {
      submitted.value = false;
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

watch(
  () => localUser.value.roleId,
  (newRoleId) => {
    if (!newRoleId) return;
    const role = allRoles.value.find((r) => r.id === newRoleId);
    if (!role) return;
    if (role.isSystemRole || role.isTemplate || role.clinicId) {
      localUser.value.clinicId = null;
    }
  }
);

// =========================================================
// HANDLERS
// =========================================================
function onPrevious() {
  if (activeTabIndex.value > 0) activeTabIndex.value--;
}

function onNext() {
  submitted.value = true;
  let isValid = false;
  if (activeTabIndex.value === 0) isValid = isBasicDetailsValid.value;
  else if (activeTabIndex.value === 1)
    isValid =
      isRoleDetailsValid.value &&
      (shouldDisableClinicDropdown.value || !!localUser.value.clinicId);
  if (isValid) {
    submitted.value = false;
    activeTabIndex.value++;
  }
}

async function onSave() {
  submitted.value = true;

  if (!isFormValid.value) {
    if (!isBasicDetailsValid.value) activeTabIndex.value = 0;
    else if (!isRoleDetailsValid.value) activeTabIndex.value = 1;
    else activeTabIndex.value = 2;
    return;
  }

  saving.value = true;

  const dto: any = {
    fullName: localUser.value.fullName,
    email: localUser.value.email,
    roleId: localUser.value.roleId,
    clinicId: localUser.value.clinicId || undefined,
    isActive: localUser.value.isActive,
  };

  if (!localUser.value.id) dto.password = localUser.value.password;

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
            <label class="block font-medium mb-2"
              >Email <span class="text-red-500">*</span></label
            >
            <InputText
              v-model.trim="localUser.email"
              placeholder="user@example.com"
              type="email"
              :invalid="
                submitted &&
                (!localUser.email || !isEmailValid(localUser.email))
              "
              class="w-full"
            />
            <small v-if="submitted && !localUser.email" class="text-red-500"
              >Email is required.</small
            >
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
            <label class="block font-medium mb-2"
              >Role <span class="text-red-500">*</span></label
            >
            <Select
              v-model="localUser.roleId"
              :options="allRoles"
              optionLabel="name"
              optionValue="id"
              placeholder="Select a role"
              :loading="rolesLoading"
              :invalid="submitted && activeTabIndex === 1 && !localUser.roleId"
              class="w-full"
              filter
              filterPlaceholder="Search roles..."
              :disabled="rolesError"
            >
              <template #value="{ value }">
                <div
                  v-if="value"
                  class="flex items-center gap-2 truncate"
                  style="width: 100%"
                >
                  <div
                    class="w-6 h-6 flex items-center justify-center rounded-md text-primary bg-primary/10 flex-shrink-0"
                  >
                    <i :class="selectedRole?.iconClass || 'pi pi-shield'"></i>
                  </div>
                  <span class="font-medium text-sm truncate">{{
                    selectedRole?.name
                  }}</span>
                </div>
                <span v-else class="text-surface-500 text-sm"
                  >Select a role</span
                >
              </template>

              <template #option="{ option, selected }">
                <div
                  class="flex flex-col gap-1.5 p-3 rounded-md transition-all duration-150 border last:border-0 cursor-pointer"
                  style="width: 100%"
                  :class="[
                    selected
                      ? 'border-primary bg-primary/5 dark:bg-primary/10 shadow-sm'
                      : 'border-transparent hover:bg-surface-50 dark:hover:bg-surface-800 hover:border-surface-200 dark:hover:border-surface-700',
                  ]"
                >
                  <div class="flex items-center justify-between gap-2">
                    <div class="flex items-center gap-2 min-w-0">
                      <div
                        class="w-7 h-7 flex items-center justify-center rounded-md bg-primary/10 text-primary flex-shrink-0"
                      >
                        <i :class="option.iconClass || 'pi pi-shield'"></i>
                      </div>
                      <span class="font-semibold text-sm truncate">{{
                        option.name
                      }}</span>
                    </div>

                    <Tag
                      v-if="option.isSystemRole"
                      value="System"
                      severity="danger"
                      class="text-xs px-2 py-0.5 flex-shrink-0"
                    />
                    <Tag
                      v-else-if="option.isTemplate"
                      value="Template"
                      severity="info"
                      class="text-xs px-2 py-0.5 flex-shrink-0"
                    />
                    <Tag
                      v-else-if="option.clinicId"
                      value="Clinic"
                      severity="success"
                      class="text-xs px-2 py-0.5 flex-shrink-0"
                    />
                  </div>

                  <div
                    v-if="option.description"
                    class="text-xs text-surface-500 dark:text-surface-400 truncate"
                    v-tooltip.top="option.description"
                  >
                    {{ option.description }}
                  </div>
                </div>
              </template>
            </Select>
            <small
              v-if="submitted && activeTabIndex === 1 && !localUser.roleId"
              class="text-red-500 block mt-2"
              >Role is required.</small
            >
          </div>

          <!-- Clinic -->
          <div v-if="isSuperAdmin">
            <label class="block font-medium mb-2">
              Clinic
              <span
                v-if="isClinicRequired"
                class="text-red-500 text-sm animate-pulse"
                >*</span
              >
            </label>
            <Select
              v-model="localUser.clinicId"
              :options="clinics"
              optionLabel="name"
              optionValue="id"
              placeholder="Select a clinic"
              :loading="clinicsLoading"
              showClear
              class="w-full"
              filter
              filterPlaceholder="Search clinics..."
              :disabled="clinicsError || shouldDisableClinicDropdown"
              :invalid="
                submitted &&
                activeTabIndex === 1 &&
                isClinicRequired &&
                !localUser.clinicId
              "
            >
              <template #value="{ value }">
                <div
                  v-if="value && !shouldDisableClinicDropdown"
                  class="flex items-center gap-2"
                >
                  <i class="pi pi-building text-primary"></i>
                  <span class="font-semibold">
                    {{ clinics.find((c) => c.id === value)?.name || "Unknown" }}
                  </span>
                </div>
                <span
                  v-else-if="shouldDisableClinicDropdown"
                  class="text-surface-500 italic"
                  >Not applicable</span
                >
                <span v-else class="text-surface-500">Select a clinic</span>
              </template>

              <template #option="{ option }">
                <div class="flex flex-col gap-1">
                  <div class="flex items-center gap-2">
                    <i class="pi pi-building text-green-500"></i>
                    <span class="font-semibold">{{ option.name }}</span>
                  </div>
                  <small class="text-surface-500">{{ option.email }}</small>
                </div>
              </template>
            </Select>

            <small
              v-if="
                submitted &&
                activeTabIndex === 1 &&
                isClinicRequired &&
                !localUser.clinicId
              "
              class="text-red-500 block mt-1"
            >
              Clinic is required for this role.
            </small>

            <small v-if="clinicsError" class="text-red-500 block mt-2">
              ❌ Failed to load clinics: {{ clinicsError.message }}
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

        <div class="flex flex-col gap-5 p-2">
          <!-- Active Status -->
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
        />
        <div class="flex gap-2">
          <Button
            v-if="activeTabIndex > 0"
            label="Previous"
            icon="pi pi-arrow-left"
            severity="secondary"
            @click="onPrevious"
          />
          <Button
            v-if="activeTabIndex < 2"
            label="Next"
            icon="pi pi-arrow-right"
            iconPos="right"
            @click="onNext"
          />
          <Button
            v-else
            :label="localUser.id ? 'Update User' : 'Create User'"
            icon="pi pi-check"
            :loading="saving"
            :disabled="!isFormValid"
            @click="onSave"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>
