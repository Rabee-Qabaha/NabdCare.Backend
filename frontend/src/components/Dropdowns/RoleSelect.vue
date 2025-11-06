<script setup lang="ts">
import { computed } from "vue";
import Select from "primevue/select";
import Tag from "primevue/tag";
import { useGroupedRoles } from "@/composables/query/useDropdownData";
import type { RoleResponseDto } from "@/types/backend";

const props = defineProps<{
  modelValue: string | null;
  label?: string;
  placeholder?: string;
  showLabel?: boolean;
  invalid?: boolean;
  required?: boolean;
  valueKey?: "id" | "name";
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: string | null): void;
}>();

const { data: groupedRolesData, isLoading, error } = useGroupedRoles();

const roles = computed<RoleResponseDto[]>(() => {
  const grouped = groupedRolesData.value;
  if (!grouped) return [];
  const { systemRoles = [], clinicRoles = [] } = grouped;
  return [...systemRoles, ...clinicRoles];
});

const selectedRole = computed(() => {
  const key = props.valueKey ?? "id";
  return roles.value.find((r) => r[key] === props.modelValue) ?? null;
});
</script>

<template>
  <div class="flex flex-col">
    <label v-if="showLabel" class="block font-medium mb-2">
      {{ label || "Role" }}
      <span v-if="props.required" class="text-red-500">*</span>
    </label>

    <Select
      :modelValue="modelValue"
      @update:modelValue="$emit('update:modelValue', $event)"
      :options="roles"
      optionLabel="name"
      :optionValue="props.valueKey ?? 'id'"
      :placeholder="placeholder || 'Select a role'"
      :loading="isLoading"
      class="w-full"
      filter
      filterPlaceholder="Search roles..."
      :invalid="invalid"
      :disabled="!!error"
    >
      <!-- Selected Value -->
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
          <span class="font-medium text-sm truncate">
            {{ selectedRole?.name }}
          </span>
        </div>
        <span
          v-else
          class="flex items-center gap-1 text-surface-600 dark:text-surface-400"
          >Select a role</span
        >
      </template>

      <!-- Option Template -->
      <template #option="{ option }">
        <div
          class="flex flex-col gap-1.5 p-3 rounded-md transition-all duration-150 border cursor-pointer"
          style="width: 100%"
        >
          <div class="flex items-center justify-between gap-2">
            <div class="flex items-center gap-2 min-w-0">
              <div
                class="w-7 h-7 flex items-center justify-center rounded-md bg-primary/10 text-primary flex-shrink-0"
              >
                <i :class="option.iconClass || 'pi pi-shield'"></i>
              </div>
              <span class="font-semibold text-sm truncate">
                {{ option.name }}
              </span>
            </div>
            <Tag
              :value="option.isSystemRole ? 'System' : 'User'"
              :severity="option.isSystemRole ? 'danger' : 'success'"
              class="text-xs px-2 py-0.5 flex-shrink-0"
            />
          </div>

          <div
            v-if="option.description"
            class="text-xs text-surface-500 dark:text-surface-400 truncate"
            v-tooltip.top="option.description"
            style="max-width: 100%"
          >
            {{ option.description }}
          </div>

          <div class="flex items-center gap-4 text-xs text-surface-400 mt-1">
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
  </div>
</template>
