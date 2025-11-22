<script setup lang="ts">
  import FloatLabel from 'primevue/floatlabel';
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  import { rolesApi } from '@/api/modules/roles';
  import { useQuery } from '@tanstack/vue-query';

  const props = defineProps<{
    modelValue: string | null;
    label?: string;
    invalid?: boolean;
    required?: boolean;
    valueKey?: 'id' | 'name';
    showClear?: boolean;
    clinicId?: string | null;
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
  }>();

  /* -----------------------------------------------------------
   LOAD ROLES SMARTLY
   If clinicId → fetch template + clinic roles
   Else → fetch ALL roles (system + clinic + template)
----------------------------------------------------------- */

  const rolesQuery = useQuery({
    queryKey: ['dropdown', 'roles', props.clinicId ?? 'system'],
    queryFn: async () => {
      // SYSTEM MODE (clinicId is null)
      if (!props.clinicId) {
        const grouped = await rolesApi.getGrouped();
        return [...grouped.systemRoles, ...grouped.templateRoles, ...grouped.clinicRoles];
      }

      // CLINIC MODE
      const [templates, clinicRoles] = await Promise.all([
        rolesApi.getTemplates().then((r) => r.data),
        rolesApi.getClinicRoles(props.clinicId!).then((r) => r.data),
      ]);

      return [...templates, ...clinicRoles];
    },
    staleTime: 1000 * 60 * 5,
  });

  const roles = computed(() => rolesQuery.data?.value ?? []);

  const selectedRole = computed(() => {
    const key = props.valueKey ?? 'id';
    return roles.value.find((r) => r[key] === props.modelValue) ?? null;
  });
</script>

<template>
  <FloatLabel variant="on" class="w-full">
    <Select
      size="medium"
      class="w-full"
      :modelValue="modelValue"
      @update:modelValue="emit('update:modelValue', $event)"
      :options="roles"
      optionLabel="name"
      :optionValue="props.valueKey ?? 'id'"
      :loading="rolesQuery.isLoading.value"
      filter
      filterPlaceholder="Search roles..."
      :invalid="props.invalid"
      :disabled="rolesQuery.isError.value"
      :showClear="props.showClear ?? false"
    >
      <!-- Selected Value -->
      <template #value="{ value }">
        <div v-if="selectedRole" class="flex items-center gap-2 truncate" style="width: 100%">
          <div
            class="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary"
          >
            <i :class="selectedRole.iconClass || 'pi pi-shield'"></i>
          </div>
          <span class="truncate text-sm font-medium">
            {{ selectedRole.name }}
          </span>
        </div>
      </template>

      <!-- Option Template -->
      <template #option="{ option }">
        <div
          class="flex cursor-pointer flex-col gap-1.5 rounded-md border p-3 transition-all duration-150"
          style="width: 100%"
        >
          <div class="flex items-center justify-between gap-2">
            <div class="flex min-w-0 items-center gap-2">
              <div
                class="flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary"
              >
                <i :class="option.iconClass || 'pi pi-shield'"></i>
              </div>

              <span class="truncate text-sm font-semibold">
                {{ option.name }}
              </span>
            </div>

            <Tag
              :value="option.isSystemRole ? 'System' : option.isTemplate ? 'Template' : 'Clinic'"
              :severity="option.isSystemRole ? 'danger' : option.isTemplate ? 'info' : 'success'"
              class="flex-shrink-0 px-2 py-0.5 text-xs"
            />
          </div>

          <div
            v-if="option.description"
            class="truncate text-xs text-surface-500 dark:text-surface-400"
            v-tooltip.top="option.description"
          >
            {{ option.description }}
          </div>

          <div class="mt-1 flex items-center gap-4 text-xs text-surface-400">
            <span
              v-if="option.userCount"
              class="flex items-center gap-1"
              v-tooltip.top="`${option.userCount} assigned users`"
            >
              <i class="pi pi-users text-xs"></i>
              {{ option.userCount }}
            </span>

            <span
              v-if="option.permissionCount"
              class="flex items-center gap-1"
              v-tooltip.top="`${option.permissionCount} permissions`"
            >
              <i class="pi pi-lock text-xs"></i>
              {{ option.permissionCount }}
            </span>
          </div>
        </div>
      </template>
    </Select>

    <label>
      {{ label || 'Select a Role' }}
      <span v-if="props.required" class="text-red-500">*</span>
    </label>
  </FloatLabel>
</template>
