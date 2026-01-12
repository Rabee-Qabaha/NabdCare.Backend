<script setup lang="ts">
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  import { useAllRoles } from '@/composables/query/roles/useRoles';
  import type { RoleResponseDto } from '@/types/backend';

  const props = withDefaults(
    defineProps<{
      modelValue: string | null;
      placeholder?: string;
      invalid?: boolean;
      valueKey?: 'id' | 'name';
      showClear?: boolean;
      clinicId?: string | null;
      // Optional: if true, filters list to only show templates
      isTemplate?: boolean;
    }>(),
    {
      valueKey: 'id',
      showClear: true,
      clinicId: null,
      isTemplate: undefined,
    },
  );

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
    (e: 'change', role: RoleResponseDto | null): void;
  }>();

  // -----------------------------------------------------------
  // DATA FETCHING (Centralized Composable)
  // -----------------------------------------------------------
  const {
    data: rolesData,
    isLoading,
    isError,
  } = useAllRoles({
    // Pass as computed so the query reacts to prop changes
    clinicId: computed(() => props.clinicId),
    isTemplate: props.isTemplate,
  });

  const roles = computed(() => rolesData.value ?? []);

  const selectedRole = computed(() => {
    if (!props.modelValue) return null;
    return roles.value.find((r) => r[props.valueKey] === props.modelValue) ?? null;
  });

  // -----------------------------------------------------------
  // UI HELPERS
  // -----------------------------------------------------------
  function getRoleSeverity(role: RoleResponseDto) {
    if (role.isSystemRole) return 'warn'; // System = Orange/Yellow
    if (role.isTemplate) return 'info'; // Template = Blue
    return 'success'; // Clinic/Standard = Green
  }

  function getRoleLabel(role: RoleResponseDto) {
    if (role.isSystemRole) return 'System';
    if (role.isTemplate) return 'Template';
    return 'Clinic';
  }

  function handleChange(newValue: string | null) {
    emit('update:modelValue', newValue);

    const roleObj = roles.value.find((r) => r[props.valueKey] === newValue) || null;
    emit('change', roleObj);
  }
</script>

<template>
  <div class="w-full">
    <Select
      :model-value="modelValue"
      :options="roles"
      option-label="name"
      :option-value="valueKey"
      :loading="isLoading"
      filter
      filter-placeholder="Search roles..."
      :invalid="invalid"
      :disabled="isError"
      :show-clear="showClear"
      :placeholder="placeholder || 'Select a Role'"
      class="w-full"
      :fluid="true"
      :pt="{
        root: { class: 'flex items-center w-full' },
        label: { class: 'flex items-center w-full overflow-hidden' },
        list: { class: 'p-1' },
      }"
      @update:model-value="handleChange"
    >
      <template #value>
        <div v-if="selectedRole" class="flex items-center gap-2 w-full overflow-hidden">
          <div
            class="flex h-5 w-5 flex-shrink-0 items-center justify-center rounded text-white text-[10px] shadow-sm"
            :style="{ backgroundColor: selectedRole.colorCode || '#64748b' }"
          >
            <i :class="selectedRole.iconClass || 'pi pi-briefcase'" style="font-size: 0.6rem"></i>
          </div>

          <span class="truncate text-sm font-medium text-surface-900 dark:text-surface-0">
            {{ selectedRole.name }}
          </span>
        </div>

        <span v-else class="text-surface-500 dark:text-surface-400 truncate">
          {{ placeholder || 'Select a Role' }}
        </span>
      </template>

      <template #option="{ option }">
        <div class="flex flex-col gap-1 w-0 min-w-full">
          <div class="flex items-center justify-between w-full gap-2">
            <div class="flex items-center gap-2 min-w-0 flex-1 overflow-hidden">
              <div
                class="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded text-white shadow-sm"
                :style="{ backgroundColor: option.colorCode || '#64748b' }"
              >
                <i :class="option.iconClass || 'pi pi-briefcase'" class="text-xs"></i>
              </div>

              <span
                class="truncate font-medium text-surface-900 dark:text-surface-0"
                :title="option.name"
              >
                {{ option.name }}
              </span>
            </div>

            <Tag
              :value="getRoleLabel(option)"
              :severity="getRoleSeverity(option)"
              class="!text-[10px] !px-1.5 !py-0 uppercase font-bold shrink-0"
            />
          </div>

          <div
            v-if="option.description"
            v-tooltip.bottom="option.description"
            class="text-xs text-surface-500 dark:text-surface-400 truncate pl-8 opacity-80 w-full"
          >
            {{ option.description }}
          </div>
        </div>
      </template>
    </Select>

    <small v-if="invalid" class="text-red-500 text-xs mt-1 ml-1">Selection is required.</small>
  </div>
</template>
