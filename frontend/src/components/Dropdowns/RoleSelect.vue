<script setup lang="ts">
  import FloatLabel from 'primevue/floatlabel';
  import Select from 'primevue/select';
  import Tag from 'primevue/tag';
  import { computed } from 'vue';

  import { useGroupedRoles } from '@/composables/query/useDropdownData';
  import type { RoleResponseDto } from '@/types/backend';

  const props = defineProps<{
    modelValue: string | null;
    label?: string;
    invalid?: boolean;
    required?: boolean;
    valueKey?: 'id' | 'name';
    showClear?: boolean;
  }>();

  const emit = defineEmits<{
    (e: 'update:modelValue', value: string | null): void;
  }>();

  const { data: groupedRolesData, isLoading, error } = useGroupedRoles();

  const roles = computed<RoleResponseDto[]>(() => {
    const grouped = groupedRolesData.value;
    if (!grouped) return [];
    return [...(grouped.systemRoles || []), ...(grouped.clinicRoles || [])];
  });

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
      :loading="isLoading"
      filter
      filterPlaceholder="Search roles..."
      :invalid="props.invalid"
      :disabled="!!error"
      :showClear="props.showClear ?? false"
    >
      <!-- Selected Value -->
      <template #value="{ value }">
        <div v-if="value" class="flex items-center gap-2 truncate" style="width: 100%">
          <div
            class="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary"
          >
            <i :class="selectedRole?.iconClass || 'pi pi-shield'"></i>
          </div>
          <span class="truncate text-sm font-medium">
            {{ selectedRole?.name }}
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
              :value="option.isSystemRole ? 'System' : 'User'"
              :severity="option.isSystemRole ? 'danger' : 'success'"
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
              v-tooltip.top="`${option.permissionCount} granted permissions`"
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
