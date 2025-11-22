// src/components/Role/RoleCreateEditDialog.vue
<template>
  <Dialog
    v-model:visible="visible"
    :modal="true"
    :closable="!isSubmitting"
    :style="{ width: '600px' }"
    class="role-create-edit-dialog"
    @hide="onClose"
  >
    <template #header>
      <div class="flex flex-col gap-1">
        <h3 class="text-lg font-semibold">
          {{ dialogTitle }}
        </h3>
        <p class="text-sm text-surface-500">
          {{ dialogSubtitle }}
        </p>
      </div>
    </template>

    <form class="flex flex-col gap-4" @submit.prevent="onSubmit">
      <!-- Name -->
      <div class="flex flex-col gap-1">
        <label class="text-sm font-medium">
          Role name
          <span class="text-red-500">*</span>
        </label>
        <InputText
          v-model="form.name"
          class="w-full"
          :class="{ 'p-invalid': errors.name }"
          placeholder="e.g. Receptionist, Doctor, Admin"
        />
        <small v-if="errors.name" class="p-error">
          {{ errors.name }}
        </small>
      </div>

      <!-- Description -->
      <div class="flex flex-col gap-1">
        <label class="text-sm font-medium">Description</label>
        <Textarea
          v-model="form.description"
          rows="3"
          auto-resize
          class="w-full"
          placeholder="Short description for this role"
        />
      </div>

      <!-- Layout: left (basic meta), right (icon/color) -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- LEFT SIDE: meta fields -->
        <div class="flex flex-col gap-3">
          <!-- SuperAdmin-only fields -->
          <div v-if="isSuperAdmin" class="flex flex-col gap-3">
            <!-- Is Template -->
            <div class="flex items-center gap-2">
              <Checkbox v-model="form.isTemplate" binary input-id="isTemplate" />
              <label for="isTemplate" class="text-sm">
                Template role (can be cloned by clinics)
              </label>
            </div>

            <!-- Display Order -->
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Display order</label>
              <InputNumber v-model="form.displayOrder" class="w-full" :min="1" :max="1000" />
              <small class="text-xs text-surface-500">
                Lower numbers are shown first in role lists.
              </small>
            </div>

            <!-- Clinic selector (placeholder) -->
            <!--
              NOTE:
              - For SuperAdmin you can plug a real clinic dropdown here
              - For now we keep a simple text hint; you can replace with PrimeVue Dropdown
            -->
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Clinic (optional)</label>
              <InputText
                v-model="form.clinicId"
                class="w-full"
                placeholder="Leave empty for system-level or current clinic"
              />
              <small class="text-xs text-surface-500">
                For SuperAdmin: set a specific clinic ID or leave empty to use current context.
              </small>
            </div>
          </div>

          <!-- Non-SuperAdmin info -->
          <div v-else class="text-xs text-surface-500">
            Role will be created for your current clinic.
          </div>
        </div>

        <!-- RIGHT SIDE: icon + color -->
        <div class="flex flex-col gap-3">
          <!-- Color palette -->
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">Color</label>
            <div class="flex flex-wrap gap-2">
              <button
                v-for="color in COLOR_PALETTE"
                :key="color"
                type="button"
                class="w-7 h-7 rounded-full border border-surface-300 flex items-center justify-center cursor-pointer transition-all"
                :style="{ backgroundColor: color }"
                :class="{
                  'ring-2 ring-primary ring-offset-2': form.colorCode === color,
                }"
                @click="selectColor(color)"
              >
                <i v-if="form.colorCode === color" class="pi pi-check text-xs text-white"></i>
              </button>
            </div>
            <div class="flex items-center gap-2 mt-1">
              <InputText v-model="form.colorCode" class="w-32 text-xs" placeholder="#3B82F6" />
              <small class="text-xs text-surface-500">Optional hex color.</small>
            </div>
          </div>

          <!-- Icon picker -->
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">Icon</label>

            <InputText v-model="iconSearch" class="w-full mb-2" placeholder="Search icon..." />

            <div
              class="grid grid-cols-5 gap-2 max-h-40 overflow-y-auto border border-surface-200 rounded-md p-2"
            >
              <button
                v-for="icon in filteredIcons"
                :key="icon.class"
                type="button"
                class="flex flex-col items-center justify-center gap-1 p-2 rounded-md border text-xs cursor-pointer transition-all"
                :class="{
                  'border-primary ring-2 ring-primary ring-offset-2': form.iconClass === icon.class,
                  'border-surface-200 hover:border-primary-300': form.iconClass !== icon.class,
                }"
                @click="selectIcon(icon.class)"
              >
                <i :class="icon.class" class="text-lg"></i>
                <span class="truncate max-w-[80px]">
                  {{ icon.label }}
                </span>
              </button>
            </div>

            <small class="text-xs text-surface-500 mt-1">
              Optional. Pick an icon for this role (e.g. doctor, admin, finance).
            </small>
          </div>
        </div>
      </div>
    </form>

    <template #footer>
      <div class="flex justify-end gap-2">
        <Button
          label="Cancel"
          severity="secondary"
          class="p-button-outlined"
          :disabled="isSubmitting"
          @click="onClose"
        />
        <Button
          :label="submitButtonLabel"
          :loading="isSubmitting"
          icon="pi pi-check"
          @click="onSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import { useRoleActions } from '@/composables/query/roles/useRoleActions';
  import { useAuthStore } from '@/stores/authStore';
  import type {
    CreateRoleRequestDto,
    RoleResponseDto,
    UpdateRoleRequestDto,
  } from '@/types/backend';
  import { computed, reactive, ref, watch } from 'vue';

  // ---------- Props / Emits ----------

  interface Props {
    visible: boolean;
    role?: RoleResponseDto | null;
    mode?: 'create' | 'edit' | 'clone';
  }

  const props = defineProps<Props>();

  const emit = defineEmits<{
    (e: 'update:visible', value: boolean): void;
    (e: 'saved'): void;
  }>();

  const visible = computed({
    get: () => props.visible,
    set: (v: boolean) => emit('update:visible', v),
  });

  // ---------- Stores & actions ----------

  const authStore = useAuthStore();
  const isSuperAdmin = computed(() => authStore.isSuperAdmin);

  const { createMutation, updateMutation, canCreateRole, canEditRole } = useRoleActions();

  // ---------- Local form state ----------

  interface RoleFormState {
    name: string;
    description: string | null;
    colorCode: string | null;
    iconClass: string | null;
    isTemplate: boolean;
    displayOrder: number;
    clinicId: string | null;
  }

  const form = reactive<RoleFormState>({
    name: '',
    description: null,
    colorCode: '#3B82F6',
    iconClass: null,
    isTemplate: false,
    displayOrder: 100,
    clinicId: null,
  });

  const errors = ref<Partial<Record<keyof RoleFormState, string>>>({});

  // ---------- Icon data ----------

  const ICON_OPTIONS = [
    { label: 'Doctor', class: 'fa-solid fa-user-doctor' },
    { label: 'Nurse', class: 'fa-solid fa-user-nurse' },
    { label: 'Reception', class: 'fa-solid fa-bell-concierge' },
    { label: 'Admin', class: 'fa-solid fa-user-gear' },
    { label: 'Finance', class: 'fa-solid fa-file-invoice-dollar' },
    { label: 'Reports', class: 'fa-solid fa-chart-line' },
    { label: 'Settings', class: 'fa-solid fa-sliders' },
    { label: 'Clinic', class: 'fa-solid fa-hospital' },
    { label: 'Patients', class: 'fa-solid fa-user-injured' },
    { label: 'Calendar', class: 'fa-solid fa-calendar-days' },
    { label: 'Security', class: 'fa-solid fa-shield-halved' },
    { label: 'Support', class: 'fa-solid fa-headset' },
  ];

  const COLOR_PALETTE = [
    '#3B82F6', // blue
    '#10B981', // green
    '#F59E0B', // amber
    '#EF4444', // red
    '#8B5CF6', // violet
    '#06B6D4', // cyan
    '#EC4899', // pink
    '#6B7280', // gray
  ];

  const iconSearch = ref('');

  const filteredIcons = computed(() => {
    const term = iconSearch.value.trim().toLowerCase();
    if (!term) return ICON_OPTIONS;

    return ICON_OPTIONS.filter(
      (i) => i.label.toLowerCase().includes(term) || i.class.toLowerCase().includes(term),
    );
  });

  // ---------- Mode helpers ----------

  const effectiveMode = computed<'create' | 'edit' | 'clone'>(() => {
    if (props.mode) return props.mode;
    if (props.role && props.role.id) return 'edit';
    return 'create';
  });

  const dialogTitle = computed(() => {
    if (effectiveMode.value === 'edit') return 'Edit role';
    if (effectiveMode.value === 'clone') return 'Clone role';
    return 'Create role';
  });

  const dialogSubtitle = computed(() => {
    if (effectiveMode.value === 'edit') return 'Update role details and presentation.';
    if (effectiveMode.value === 'clone') return 'Create a new role based on an existing one.';
    return 'Define a new role for your clinic or system.';
  });

  const submitButtonLabel = computed(() => {
    if (effectiveMode.value === 'edit') return 'Save changes';
    if (effectiveMode.value === 'clone') return 'Create cloned role';
    return 'Create role';
  });

  const isSubmitting = computed(
    () => createMutation.isPending.value || updateMutation.isPending.value,
  );

  // ---------- Init / reset ----------

  function resetForm() {
    form.name = '';
    form.description = null;
    form.colorCode = '#3B82F6';
    form.iconClass = null;
    form.isTemplate = false;
    form.displayOrder = 100;
    form.clinicId = null;
    errors.value = {};
  }

  function loadFromRole(role: RoleResponseDto | null | undefined, isClone = false) {
    if (!role) {
      resetForm();
      return;
    }

    form.name = isClone ? `${role.name} (Copy)` : role.name || '';
    form.description = role.description ?? null;
    form.colorCode = role.colorCode ?? '#3B82F6';
    form.iconClass = role.iconClass ?? null;
    form.isTemplate = role.isTemplate ?? false;
    form.displayOrder = role.displayOrder ?? 100;
    form.clinicId = role.clinicId ?? null;
    errors.value = {};
  }

  watch(
    () => props.visible,
    (v) => {
      if (v) {
        if (effectiveMode.value === 'edit') {
          loadFromRole(props.role, false);
        } else if (effectiveMode.value === 'clone') {
          loadFromRole(props.role, true);
        } else {
          resetForm();
        }
      }
    },
    { immediate: false },
  );

  // ---------- Field helpers ----------

  function selectIcon(iconClass: string) {
    form.iconClass = iconClass;
  }

  function selectColor(color: string) {
    form.colorCode = color;
  }

  // ---------- Validation ----------

  function validateForm(): boolean {
    const nextErrors: Partial<Record<keyof RoleFormState, string>> = {};

    const name = form.name.trim();
    if (!name) {
      nextErrors.name = 'Role name is required.';
    } else if (name.length < 3) {
      nextErrors.name = 'Role name must be at least 3 characters.';
    }

    if (form.displayOrder <= 0) {
      nextErrors.displayOrder = 'Display order must be greater than 0.';
    }

    errors.value = nextErrors;
    return Object.keys(nextErrors).length === 0;
  }

  // ---------- Submit / close ----------

  function onClose() {
    if (isSubmitting.value) return;
    visible.value = false;
  }

  function onSubmit() {
    if (!validateForm()) return;

    const trimmedName = form.name.trim();

    if (effectiveMode.value === 'edit') {
      if (!props.role?.id) return;
      if (!canEditRole) return;

      const payload: UpdateRoleRequestDto = {
        // TS may complain until TypeGen is regenerated against the new C# DTO
        name: trimmedName,
        description: form.description ?? '',
        displayOrder: form.displayOrder,
        colorCode: form.colorCode ?? '',
        iconClass: form.iconClass ?? '',
        isTemplate: isSuperAdmin.value ? form.isTemplate : (props.role.isTemplate ?? false),
      } as unknown as UpdateRoleRequestDto;

      updateMutation.mutate(
        {
          roleId: props.role.id,
          data: payload,
        },
        {
          onSuccess: () => {
            emit('saved');
            onClose();
          },
        },
      );

      return;
    }

    // create / clone
    if (!canCreateRole) return;

    const createPayload: CreateRoleRequestDto = {
      // TS may complain until TypeGen is regenerated against the new C# DTO
      name: trimmedName,
      description: form.description ?? '',
      clinicId: isSuperAdmin.value ? form.clinicId || null : null,
      isTemplate: isSuperAdmin.value ? form.isTemplate : false,
      displayOrder: form.displayOrder,
      colorCode: form.colorCode ?? '',
      iconClass: form.iconClass ?? '',
      templateRoleId: effectiveMode.value === 'clone' && props.role?.id ? props.role.id : null,
    } as unknown as CreateRoleRequestDto;

    createMutation.mutate(createPayload, {
      onSuccess: () => {
        emit('saved');
        onClose();
      },
    });
  }
</script>

<style scoped>
  .role-create-edit-dialog :deep(.p-dialog-content) {
    padding-top: 0.75rem;
  }
</style>
