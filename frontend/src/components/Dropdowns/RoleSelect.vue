<script setup lang="ts">
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  import { rolesApi } from '@/api/modules/roles';
  import type { RoleResponseDto } from '@/types/backend';
  import { useQuery } from '@tanstack/vue-query';

  const props = withDefaults(
    defineProps<{
      modelValue: string | null;
      placeholder?: string;
      invalid?: boolean;
      valueKey?: 'id' | 'name';
      showClear?: boolean;
      clinicId?: string | null;
    }>(),
    {
      valueKey: 'id',
      showClear: true,
      clinicId: null,
    },
  );

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
  }>();

  // -----------------------------------------------------------
  // DATA FETCHING
  // -----------------------------------------------------------
  const rolesQuery = useQuery({
    queryKey: ['dropdown', 'roles', props.clinicId ?? 'global'],
    queryFn: async () => {
      const res = await rolesApi.getAll({
        clinicId: props.clinicId,
        includeDeleted: false,
      });
      return res.data || [];
    },
    staleTime: 1000 * 60 * 5,
  });

  const roles = computed(() => rolesQuery.data.value ?? []);

  const selectedRole = computed(() => {
    if (!props.modelValue) return null;
    return roles.value.find((r) => r[props.valueKey] === props.modelValue) ?? null;
  });

  function getRoleSeverity(role: RoleResponseDto) {
    if (role.isSystemRole) return 'warn';
    if (role.isTemplate) return 'info';
    return 'success';
  }

  function getRoleLabel(role: RoleResponseDto) {
    if (role.isSystemRole) return 'System';
    if (role.isTemplate) return 'Clinic';
    return 'Clinic';
  }
</script>

<template>
  <div class="w-full">
    <Select
      :modelValue="modelValue"
      @update:modelValue="emit('update:modelValue', $event)"
      :options="roles"
      optionLabel="name"
      :optionValue="valueKey"
      :loading="rolesQuery.isLoading.value"
      filter
      filterPlaceholder="Search roles..."
      :invalid="invalid"
      :disabled="rolesQuery.isError.value"
      :showClear="showClear"
      :placeholder="placeholder || 'Select a Role'"
      class="w-full"
      :fluid="true"
      :pt="{
        root: { class: 'flex items-center w-full' },
        label: { class: 'flex items-center w-full overflow-hidden' },
        list: { class: 'p-1' },
      }"
    >
      <template #value="{ value }">
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
            class="text-xs text-surface-500 dark:text-surface-400 truncate pl-8 opacity-80 w-full"
            v-tooltip.bottom="option.description"
          >
            {{ option.description || 'No description' }}
          </div>
        </div>
      </template>
    </Select>

    <small v-if="invalid" class="text-red-500 text-xs mt-1 ml-1">Selection is required.</small>
  </div>
</template>
