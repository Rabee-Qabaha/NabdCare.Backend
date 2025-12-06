<template>
  <Dialog
    v-model:visible="visible"
    :modal="true"
    :closable="!isSubmitting"
    :style="{ width: '650px', maxWidth: '95vw' }"
    :pt="{
      root: { class: 'rounded-xl border-0 shadow-2xl overflow-hidden' },
      header: {
        class:
          'border-b border-surface-200/50 dark:border-surface-700/50 py-4 px-6 bg-surface-0 dark:bg-surface-900',
      },
      content: { class: 'p-0 bg-surface-0 dark:bg-surface-900' },
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
          class="flex items-center justify-center w-10 h-10 rounded-full shrink-0 text-white transition-all duration-300"
          :style="{ backgroundColor: form.colorCode || '#64748b' }"
        >
          <i :class="[form.iconClass || 'pi pi-plus', 'text-lg']"></i>
        </div>
        <div class="flex flex-col">
          <h3 class="text-lg font-bold text-surface-900 dark:text-surface-0 leading-tight">
            {{ dialogTitle }}
          </h3>
          <p class="text-sm text-surface-500 dark:text-surface-400">
            {{ dialogSubtitle }}
          </p>
        </div>
      </div>
    </template>

    <form @submit.prevent="onSubmit" class="flex flex-col h-full">
      <div
        class="p-6 flex flex-col gap-6 overflow-y-auto custom-scrollbar"
        style="max-height: 600px"
      >
        <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
          <div class="flex flex-col gap-1.5">
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

          <div v-if="isSuperAdmin" class="flex flex-col gap-1.5">
            <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
              Target Clinic
              <span class="text-surface-400 font-normal">(Optional)</span>
            </label>
            <ClinicSelect
              v-model="form.clinicId"
              placeholder="Global (System Level)"
              class="w-full"
              :showClear="true"
            />
          </div>
          <div v-else class="flex flex-col gap-1.5">
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

        <div class="flex flex-col gap-1.5">
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

        <Divider class="my-0 border-surface-200 dark:border-surface-700" />

        <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div class="flex flex-col gap-2">
            <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
              Role Color
              <span v-if="isLoadingRoles" class="text-[10px] font-normal text-surface-400 ml-2">
                (Loading...)
              </span>
            </label>

            <div class="flex items-center gap-2 overflow-x-auto pb-2 custom-scrollbar flex-nowrap">
              <div
                class="relative w-8 h-8 rounded-full border border-dashed border-surface-300 dark:border-surface-600 flex items-center justify-center text-surface-400 hover:bg-surface-50 dark:hover:bg-surface-800 cursor-pointer transition-colors shrink-0"
                title="Custom Color"
              >
                <input
                  type="color"
                  v-model="form.colorCode"
                  class="absolute inset-0 opacity-0 cursor-pointer"
                />
                <i class="pi pi-palette text-xs"></i>
              </div>

              <button
                v-for="color in availableColors"
                :key="color"
                type="button"
                class="w-8 h-8 rounded-full border border-surface-200 dark:border-surface-700 shadow-sm flex items-center justify-center cursor-pointer transition-transform hover:scale-110 shrink-0 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-surface-300 dark:focus:ring-surface-600"
                :style="{ backgroundColor: color }"
                @click="form.colorCode = color"
              >
                <i
                  v-if="form.colorCode === color"
                  class="pi pi-check text-xs text-white font-bold shadow-sm"
                ></i>
              </button>
            </div>
          </div>

          <div class="flex flex-col gap-2">
            <label class="text-sm font-semibold text-surface-700 dark:text-surface-200">
              Role Icon
            </label>
            <Dropdown
              v-model="form.iconClass"
              :options="availableIcons"
              optionLabel="label"
              optionValue="class"
              placeholder="Select an icon"
              class="w-full"
              filter
              :virtualScrollerOptions="{ itemSize: 38 }"
              :disabled="effectiveMode === 'clone'"
              :class="{ 'opacity-70 bg-surface-50 dark:bg-surface-800': effectiveMode === 'clone' }"
            >
              <template #value="slotProps">
                <div v-if="slotProps.value" class="flex items-center gap-2">
                  <i
                    :class="[slotProps.value, 'text-surface-600 dark:text-surface-200 text-lg']"
                  ></i>
                  <span>{{ getIconLabel(slotProps.value) }}</span>
                </div>
                <span v-else>{{ slotProps.placeholder }}</span>
              </template>
              <template #option="slotProps">
                <div class="flex items-center gap-3">
                  <i
                    :class="[
                      slotProps.option.class,
                      'text-surface-500 dark:text-surface-400 text-lg w-6 text-center',
                    ]"
                  ></i>
                  <span>{{ slotProps.option.label }}</span>
                </div>
              </template>
            </Dropdown>

            <small
              v-if="availableIcons.length < ICON_OPTIONS.length"
              class="text-[10px] text-surface-400"
            >
              Icons assigned to other roles are hidden.
            </small>
          </div>
        </div>

        <div
          v-if="isSuperAdmin"
          class="mt-3 p-4 bg-surface-50 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700"
        >
          <div class="flex flex-col gap-3">
            <h4
              class="text-[10px] font-bold text-surface-400 dark:text-surface-500 uppercase tracking-widest flex items-center gap-2"
            >
              <i class="pi pi-sliders-h text-[10px]"></i>
              System Configuration
            </h4>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="flex flex-col gap-1.5">
                <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
                  Sort Order
                </label>
                <InputNumber
                  v-model="form.displayOrder"
                  :min="1"
                  :max="1000"
                  showButtons
                  buttonLayout="horizontal"
                  :step="1"
                  input-class="!w-full !text-center !text-xs font-medium"
                  class="w-full"
                  :pt="{
                    root: { class: 'h-[36px]' },
                    input: { class: '!py-1' },
                    incrementButton: {
                      class:
                        '!w-8 !bg-white dark:!bg-surface-800 !text-surface-500 !border-surface-300',
                    },
                    decrementButton: {
                      class:
                        '!w-8 !bg-white dark:!bg-surface-800 !text-surface-500 !border-surface-300',
                    },
                  }"
                >
                  <template #incrementbuttonicon><i class="pi pi-plus text-[10px]" /></template>
                  <template #decrementbuttonicon><i class="pi pi-minus text-[10px]" /></template>
                </InputNumber>
              </div>

              <div class="flex flex-col gap-1.5">
                <label class="text-xs font-medium text-surface-600 dark:text-surface-300">
                  Role Behavior
                </label>

                <div
                  class="h-[36px] flex items-center px-3 rounded-md border border-surface-300 dark:border-surface-700 bg-white dark:bg-surface-800 hover:border-primary-500 dark:hover:border-primary-400 transition-all cursor-pointer group select-none"
                  @click="form.isTemplate = !form.isTemplate"
                >
                  <Checkbox
                    v-model="form.isTemplate"
                    binary
                    input-id="isTemplate"
                    class="pointer-events-none scale-75 -ml-1"
                  />
                  <label
                    class="ml-2 text-xs font-medium text-surface-700 dark:text-surface-200 cursor-pointer group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors"
                  >
                    System Template
                  </label>

                  <span v-if="form.isTemplate" class="ml-auto flex h-2 w-2">
                    <span
                      class="animate-ping absolute inline-flex h-2 w-2 rounded-full bg-green-400 opacity-75"
                    ></span>
                    <span class="relative inline-flex rounded-full h-2 w-2 bg-green-500"></span>
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </form>

    <template #footer>
      <div class="flex justify-end gap-2 w-full mt-4">
        <Button
          label="Cancel"
          severity="secondary"
          outlined
          class="w-full sm:w-auto"
          :disabled="isSubmitting"
          @click="onClose"
        />
        <Button
          :label="submitButtonLabel"
          :loading="isSubmitting"
          icon="pi pi-check"
          class="w-full sm:w-auto"
          @click="onSubmit"
        />
      </div>
    </template>
  </Dialog>
</template>

<script setup lang="ts">
  import ClinicSelect from '@/components/Dropdowns/ClinicSelect.vue';
  import Button from 'primevue/button';
  import Checkbox from 'primevue/checkbox';
  import Dialog from 'primevue/dialog';
  import Divider from 'primevue/divider';
  import Dropdown from 'primevue/dropdown';
  import InputNumber from 'primevue/inputnumber';
  import InputText from 'primevue/inputtext';
  import Textarea from 'primevue/textarea';

  import { rolesApi } from '@/api/modules/roles';
  import { useRoleActions } from '@/composables/query/roles/useRoleActions';
  import { useAuthStore } from '@/stores/authStore';
  import type { CloneRoleRequestDto, RoleResponseDto } from '@/types/backend';
  import { useQuery } from '@tanstack/vue-query';
  import { computed, reactive, ref, watch } from 'vue';

  // ... (Keep all script logic exactly the same as before) ...
  // Just copy-paste the previous script block here.
  // The logic remains identical, only the template structure changed.

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
    '#3B82F6',
    '#10B981',
    '#F59E0B',
    '#EF4444',
    '#8B5CF6',
    '#06B6D4',
    '#EC4899',
    '#6B7280',
    '#14B8A6',
    '#6366F1',
    '#D946EF',
    '#F43F5E',
    '#84CC16',
    '#EAB308',
    '#F97316',
    '#78716C',
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
      const res = await rolesApi.getAll({ includeDeleted: false });
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

  function getIconLabel(cls: string) {
    return ICON_OPTIONS.find((i) => i.class === cls)?.label || 'Custom Icon';
  }

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
    form.isTemplate = role.isTemplate ?? false;
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
