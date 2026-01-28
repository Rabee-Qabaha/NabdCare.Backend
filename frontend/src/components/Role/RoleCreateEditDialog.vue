<template>
  <BaseDrawer
    v-model:visible="visible"
    :title="dialogTitle"
    :subtitle="dialogSubtitle"
    width="md:!w-[650px]"
    :no-padding="true"
    :dismissable="false"
    @close="onClose"
  >
    <template #header>
      <div class="flex items-center gap-4">
        <div
          class="flex items-center justify-center w-12 h-12 rounded-xl shrink-0 text-white shadow-md transition-all duration-300 ring-2 ring-white dark:ring-surface-800 ring-offset-2 ring-offset-surface-100 dark:ring-offset-surface-900"
          :style="{ backgroundColor: form.colorCode || '#64748b' }"
        >
          <i :class="[form.iconClass || 'pi pi-plus', 'text-xl']"></i>
        </div>
        <div class="flex flex-col gap-1">
          <h3 class="text-xl font-bold text-surface-900 dark:text-surface-0 leading-none">
            {{ dialogTitle }}
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400 leading-snug">
            {{ dialogSubtitle }}
          </p>
        </div>
      </div>
    </template>

    <form class="flex flex-col h-full" @submit.prevent="onSubmit">
      <div class="p-6 flex flex-col gap-8">
        <!-- Basic Info Section -->
        <div class="flex flex-col gap-5">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
            <div class="flex flex-col gap-2">
              <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
                Role Name
                <span class="text-red-500">*</span>
              </label>
              <InputText
                v-model="form.name"
                class="w-full"
                :class="{ 'p-invalid': errors.name }"
                placeholder="e.g. Senior Doctor"
              />
              <small v-if="errors.name" class="text-red-500 text-xs">{{ errors.name }}</small>
            </div>

            <div v-if="isSuperAdmin" class="flex flex-col gap-2">
              <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
                Target Clinic
                <span class="text-surface-400 font-normal">(Optional)</span>
              </label>
              <ClinicSelect
                v-model="form.clinicId"
                placeholder="Global (System Level)"
                class="w-full"
                :show-clear="true"
              />
            </div>
            <div v-else class="flex flex-col gap-2">
              <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
                Scope
              </label>
              <div
                class="h-[42px] flex items-center px-3 rounded-md border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-800 text-surface-500 dark:text-surface-400 text-sm italic"
              >
                Current Clinic Only
              </div>
            </div>
          </div>

          <div class="flex flex-col gap-2">
            <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
              Description
            </label>
            <Textarea
              v-model="form.description"
              rows="2"
              auto-resize
              class="w-full"
              placeholder="Describe the role's responsibilities..."
            />
          </div>
        </div>

        <Divider class="my-0 border-surface-200 dark:border-surface-700" />

        <!-- Visual Identity Section -->
        <div class="flex flex-col gap-6">
          <RoleColorSelector
            v-model="form.colorCode"
            :colors="availableColors"
            :loading="isLoadingRoles"
          />

          <RoleIconSelector
            v-model="form.iconClass"
            :options="availableIcons"
            :disabled="effectiveMode === 'clone'"
            :has-hidden-icons="availableIcons.length < ICON_OPTIONS.length"
            hidden-message="Some icons are used by other roles."
          />
        </div>

        <!-- System Configuration Section -->
        <div
          v-if="isSuperAdmin"
          class="rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-900/50 p-5"
        >
          <h4
            class="text-xs font-bold text-surface-900 dark:text-surface-100 uppercase tracking-wider mb-4 flex items-center gap-2"
          >
            <i class="pi pi-cog text-primary-500"></i>
            System Configuration
          </h4>

          <div class="flex flex-col gap-4">
            <!-- Template Toggle Card -->
            <div
              class="flex items-center justify-between p-4 rounded-lg border border-primary-200 dark:border-primary-900/30 bg-primary-50 dark:bg-primary-900/10"
            >
              <div class="flex flex-col gap-1">
                <label
                  for="isTemplate"
                  class="text-sm font-bold text-primary-800 dark:text-primary-400 cursor-pointer"
                >
                  Save as Template
                </label>
                <p class="text-xs text-primary-600 dark:text-primary-500/80">
                  This role will be used as a template for setting up new clinics.
                </p>
              </div>
              <ToggleSwitch
                v-model="form.isTemplate"
                input-id="isTemplate"
                :pt="{
                  root: {
                    class:
                      'w-12 h-7 [&_.p-toggleswitch-slider]:bg-primary-200 [&.p-toggleswitch-checked_.p-toggleswitch-slider]:bg-primary-500',
                  },
                }"
              />
            </div>

            <!-- Display Order -->
            <div
              class="flex items-center justify-between p-4 rounded-lg border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-800"
            >
              <div class="flex flex-col gap-0.5">
                <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
                  Display Order
                </label>
                <p class="text-xs text-surface-500 dark:text-surface-400">
                  Determines the sorting priority in lists.
                </p>
              </div>
              <InputNumber
                v-model="form.displayOrder"
                :min="1"
                :max="1000"
                show-buttons
                button-layout="horizontal"
                :step="1"
                input-class="!w-16 !text-center !text-sm font-medium border-0 bg-transparent"
                class="w-auto border border-surface-300 dark:border-surface-600 rounded-md overflow-hidden"
                :pt="{
                  root: { class: 'h-[36px]' },
                  input: { class: '!py-1' },
                  incrementButton: {
                    class:
                      '!w-8 !bg-surface-50 dark:!bg-surface-700 !text-surface-600 dark:!text-surface-300 !border-l !border-surface-300 dark:!border-surface-600 hover:!bg-surface-100 dark:hover:!bg-surface-600',
                  },
                  decrementButton: {
                    class:
                      '!w-8 !bg-surface-50 dark:!bg-surface-700 !text-surface-600 dark:!text-surface-300 !border-r !border-surface-300 dark:!border-surface-600 hover:!bg-surface-100 dark:hover:!bg-surface-600',
                  },
                }"
              >
                <template #incrementbuttonicon><i class="pi pi-plus text-[10px]" /></template>
                <template #decrementbuttonicon><i class="pi pi-minus text-[10px]" /></template>
              </InputNumber>
            </div>
          </div>
        </div>
      </div>
    </form>

    <template #footer="{ close }">
      <div class="flex gap-3 w-full">
        <Button
          label="Cancel"
          severity="secondary"
          outlined
          class="!w-[30%]"
          :disabled="isSubmitting"
          @click="close"
        />
        <Button
          :label="submitButtonLabel"
          :loading="isSubmitting"
          icon="pi pi-check"
          class="flex-1"
          @click="onSubmit"
        />
      </div>
    </template>
  </BaseDrawer>
</template>

<script setup lang="ts">
  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';
  import RoleColorSelector from '@/components/Role/RoleColorSelector.vue';
  import RoleIconSelector from '@/components/Role/RoleIconSelector.vue';
  import BaseDrawer from '@/components/shared/BaseDrawer.vue';
  import Button from 'primevue/button';
  import Divider from 'primevue/divider';
  import InputNumber from 'primevue/inputnumber';
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';
  import ToggleSwitch from 'primevue/toggleswitch';

  import { rolesApi } from '@/api/modules/roles';
  import { useRoleActions } from '@/composables/query/roles/useRoleActions';
  import { useAuthStore } from '@/stores/authStore';
  import type { CloneRoleRequestDto, RoleResponseDto } from '@/types/backend';
  import { useQuery } from '@tanstack/vue-query';
  import { computed, reactive, ref, watch } from 'vue';

  // ---------- Props & Emits ----------

  interface Props {
    visible: boolean;
    role?: RoleResponseDto | null;
    mode?: 'create' | 'edit' | 'clone';
  }

  const props = defineProps<Props>();
  const emit = defineEmits<{ (e: 'update:visible', value: boolean): void; (e: 'saved'): void }>();

  const visible = computed({
    get: () => props.visible,
    set: (v: boolean) => emit('update:visible', v),
  });

  // ---------- Data ----------

  const COLOR_PALETTE = [
    '#3B82F6', // Blue
    '#10B981', // Emerald
    '#F59E0B', // Amber
    '#EF4444', // Red
    '#8B5CF6', // Violet
    '#06B6D4', // Cyan
    '#EC4899', // Pink
    '#6B7280', // Gray
    '#14B8A6', // Teal
    '#6366F1', // Indigo
    '#D946EF', // Fuchsia
    '#F43F5E', // Rose
    '#84CC16', // Lime
    '#EAB308', // Yellow
    '#F97316', // Orange
    '#78716C', // Stone
    '#22C55E', // Green
    '#0EA5E9', // Sky
    '#A855F7', // Purple
    '#FB7185', // Rose-400
    '#2DD4BF', // Teal-400
    '#94A3B8', // Slate
    '#FBbf24', // Amber-400
  ];

  const ICON_OPTIONS = [
    { label: 'User', class: 'pi pi-user' },
    { label: 'Edit User', class: 'pi pi-user-edit' },
    { label: 'Users', class: 'pi pi-users' },
    { label: 'ID Card', class: 'pi pi-id-card' },
    { label: 'Verified', class: 'pi pi-verified' },
    { label: 'Admin', class: 'pi pi-cog' },
    { label: 'Shield', class: 'pi pi-shield' },
    { label: 'Lock', class: 'pi pi-lock' },
    { label: 'Key', class: 'pi pi-key' },
    { label: 'Database', class: 'pi pi-database' },
    { label: 'Server', class: 'pi pi-server' },
    { label: 'Cloud', class: 'pi pi-cloud' },
    { label: 'Wrench', class: 'pi pi-wrench' },
    { label: 'Hammer', class: 'pi pi-hammer' },
    { label: 'Power', class: 'pi pi-power-off' },
    { label: 'Briefcase', class: 'pi pi-briefcase' },
    { label: 'Building', class: 'pi pi-building' },
    { label: 'Dollar', class: 'pi pi-dollar' },
    { label: 'Wallet', class: 'pi pi-wallet' },
    { label: 'Credit Card', class: 'pi pi-credit-card' },
    { label: 'Chart', class: 'pi pi-chart-bar' },
    { label: 'Percentage', class: 'pi pi-percentage' },
    { label: 'Calendar', class: 'pi pi-calendar' },
    { label: 'Clock', class: 'pi pi-clock' },
    { label: 'Box', class: 'pi pi-box' },
    { label: 'Truck', class: 'pi pi-truck' },
    { label: 'Cart', class: 'pi pi-shopping-cart' },
    { label: 'Map', class: 'pi pi-map' },
    { label: 'Globe', class: 'pi pi-globe' },
    { label: 'Compass', class: 'pi pi-compass' },
    { label: 'Phone', class: 'pi pi-phone' },
    { label: 'Envelope', class: 'pi pi-envelope' },
    { label: 'Inbox', class: 'pi pi-inbox' },
    { label: 'Send', class: 'pi pi-send' },
    { label: 'Comments', class: 'pi pi-comments' },
    { label: 'Bell', class: 'pi pi-bell' },
    { label: 'Share', class: 'pi pi-share-alt' },
    { label: 'Link', class: 'pi pi-link' },
    { label: 'Star', class: 'pi pi-star' },
    { label: 'Heart', class: 'pi pi-heart' },
    { label: 'Check', class: 'pi pi-check-circle' },
    { label: 'File', class: 'pi pi-file' },
    { label: 'Book', class: 'pi pi-book' },
    { label: 'Tags', class: 'pi pi-tags' },
    { label: 'Flag', class: 'pi pi-flag' },
    { label: 'Eye', class: 'pi pi-eye' },
    { label: 'Search', class: 'pi pi-search' },
    { label: 'Bolt', class: 'pi pi-bolt' },
    { label: 'Alert', class: 'pi pi-exclamation-triangle' },
    { label: 'Info', class: 'pi pi-info-circle' },
    // New Icons
    { label: 'Home', class: 'pi pi-home' },
    { label: 'Filter', class: 'pi pi-filter' },
    { label: 'List', class: 'pi pi-list' },
  ];

  // ---------- Store & Logic ----------

  const authStore = useAuthStore();
  const isSuperAdmin = computed(() => authStore.isSuperAdmin);
  const { createMutation, updateMutation, cloneMutation } = useRoleActions();

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
    iconClass: 'pi pi-user',
    isTemplate: false,
    displayOrder: 100,
    clinicId: null,
  });

  const errors = ref<Partial<Record<keyof RoleFormState, string>>>({});

  const effectiveMode = computed(() => {
    if (props.mode) return props.mode;
    if (props.role && props.role.id) return 'edit';
    return 'create';
  });

  const dialogTitle = computed(() => {
    if (effectiveMode.value === 'edit') return 'Edit Role';
    if (effectiveMode.value === 'clone') return 'Clone Role';
    return 'Create Role';
  });

  const dialogSubtitle = computed(() => {
    if (effectiveMode.value === 'edit') return 'Modify role details and appearance.';
    if (effectiveMode.value === 'clone') return 'Create a new role based on an existing one.';
    return 'Define a new role for your system.';
  });

  const submitButtonLabel = computed(() =>
    effectiveMode.value === 'edit' ? 'Save Changes' : 'Create Role',
  );
  const isSubmitting = computed(
    () =>
      createMutation.isPending.value ||
      updateMutation.isPending.value ||
      cloneMutation.isPending.value,
  );

  // ---------- Query: Fetch All Roles (For Exclusion + Max Order) ----------

  const { data: allRoles, isLoading: isLoadingRoles } = useQuery({
    queryKey: ['roles-list-all'],
    queryFn: async () => {
      // Fix: Add defaults for mandatory fields in RoleFilterRequestDto
      const res = await rolesApi.getAll({
        includeDeleted: false,
        limit: 1000,
        cursor: '',
        sortBy: '',
        descending: false,
        filter: '',
      });
      return res.data || [];
    },
    enabled: visible,
    staleTime: 1000 * 60,
  });

  // 1. Calculate Next Max Order
  const nextDisplayOrder = computed(() => {
    if (!allRoles.value || allRoles.value.length === 0) return 10;
    const max = Math.max(...allRoles.value.map((r: any) => r.displayOrder || 0));
    return max + 1;
  });

  // 2. Exclusion Logic
  const availableColors = computed(() => {
    if (!allRoles.value) return COLOR_PALETTE;
    const taken = new Set(
      allRoles.value
        .filter((r: any) => effectiveMode.value === 'create' || r.id !== props.role?.id)
        .map((r: any) => r.colorCode),
    );
    return COLOR_PALETTE.filter((c) => !taken.has(c));
  });

  const availableIcons = computed(() => {
    if (!allRoles.value) return ICON_OPTIONS;
    const taken = new Set(
      allRoles.value
        .filter((r: any) => effectiveMode.value === 'create' || r.id !== props.role?.id)
        .map((r: any) => r.iconClass),
    );
    return ICON_OPTIONS.filter(
      (i) =>
        !taken.has(i.class) ||
        (effectiveMode.value === 'clone' && props.role?.iconClass === i.class),
    );
  });

  // ---------- Helpers ----------

  function setRandomDefaults() {
    const colors = availableColors.value;
    const icons = availableIcons.value;

    // Color Selection
    if (colors.length > 0) {
      const randomIndex = Math.floor(Math.random() * colors.length);
      form.colorCode = colors[randomIndex] ?? '#3B82F6';
    } else {
      form.colorCode = COLOR_PALETTE[0] ?? '#3B82F6';
    }

    // Icon Selection
    if (icons.length > 0) {
      const randomIndex = Math.floor(Math.random() * icons.length);
      form.iconClass = icons[randomIndex]?.class ?? 'pi pi-user';
    } else {
      form.iconClass = ICON_OPTIONS[0]?.class ?? 'pi pi-user';
    }
  }

  function resetForm() {
    Object.assign(form, {
      name: '',
      description: null,
      isTemplate: false,
      displayOrder: nextDisplayOrder.value,
      clinicId: null,
    });
    setRandomDefaults();
    errors.value = {};
  }

  function loadFromRole(role: RoleResponseDto, isClone = false) {
    form.name = isClone ? `${role.name} (Copy)` : role.name || '';
    form.description = role.description ?? null;
    form.isTemplate = isClone ? false : (role.isTemplate ?? false);
    form.clinicId = role.clinicId ?? null;

    if (isClone) {
      form.displayOrder = nextDisplayOrder.value;

      // 1. Keep the same ICON (Functionality)
      form.iconClass = role.iconClass;

      // 2. Pick a NEW color (Identity)
      const otherColors = availableColors.value.filter((c) => c !== role.colorCode);
      if (otherColors.length > 0) {
        const randomIdx = Math.floor(Math.random() * otherColors.length);
        form.colorCode = otherColors[randomIdx] ?? null;
      } else {
        form.colorCode = availableColors.value[0] ?? role.colorCode;
      }
    } else {
      // Edit Mode
      form.displayOrder = role.displayOrder ?? 100;
      form.colorCode = role.colorCode;
      form.iconClass = role.iconClass;
    }
    errors.value = {};
  }

  // Watchers
  watch([() => props.visible, isLoadingRoles], ([v, loading]) => {
    if (v && !loading) {
      if (props.role) {
        loadFromRole(props.role, effectiveMode.value === 'clone');
      } else {
        resetForm();
      }
    }
  });

  // ---------- Submit ----------

  function validateForm() {
    const newErrors: any = {};
    if (!form.name?.trim()) newErrors.name = 'Name is required.';
    errors.value = newErrors;
    return Object.keys(newErrors).length === 0;
  }

  function onSubmit() {
    if (!validateForm()) return;

    const basePayload = {
      name: form.name.trim(),
      description: form.description ?? '',
      displayOrder: form.displayOrder,
      colorCode: form.colorCode ?? '',
      iconClass: form.iconClass ?? '',
      isTemplate: isSuperAdmin.value ? form.isTemplate : false,
    };

    if (effectiveMode.value === 'edit') {
      if (!props.role?.id) return;
      updateMutation.mutate(
        { roleId: props.role.id, data: basePayload },
        {
          onSuccess: () => {
            emit('saved');
            onClose();
          },
        },
      );
    } else if (effectiveMode.value === 'clone') {
      if (!props.role?.id) return;
      const clonePayload: CloneRoleRequestDto = {
        newRoleName: form.name.trim(),
        description: form.description,
        clinicId: isSuperAdmin.value ? form.clinicId || null : null,
        copyPermissions: true,
        colorCode: form.colorCode,
        iconClass: form.iconClass,
        displayOrder: form.displayOrder,
      };
      cloneMutation.mutate(
        { id: props.role.id, data: clonePayload },
        {
          onSuccess: () => {
            emit('saved');
            onClose();
          },
        },
      );
    } else {
      const createPayload: any = { ...basePayload };
      if (isSuperAdmin.value) createPayload.clinicId = form.clinicId || null;
      createMutation.mutate(createPayload, {
        onSuccess: () => {
          emit('saved');
          onClose();
        },
      });
    }
  }

  function onClose() {
    if (isSubmitting.value) return;
    visible.value = false;
  }
</script>

<style scoped>
  /* Custom scrollbar classes */
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
</style>
