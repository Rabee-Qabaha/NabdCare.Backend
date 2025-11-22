// src/composables/role/useRoleDialog.ts
import type { RoleResponseDto } from '@/types/backend';
import { computed, ref } from 'vue';

export function useRoleDialog() {
  const visible = ref(false);
  const localRole = ref<Partial<RoleResponseDto>>({});
  const submitted = ref(false);
  const saving = ref(false);

  const mode = computed(() => (localRole.value.id ? 'edit' : 'create'));

  const isNewRole = computed(() => !localRole.value.id);

  const open = (role?: Partial<RoleResponseDto>) => {
    if (role) {
      localRole.value = { ...role };
    } else {
      localRole.value = {};
    }
    submitted.value = false;
    visible.value = true;
  };

  const close = () => {
    visible.value = false;
    submitted.value = false;
    localRole.value = {};
  };

  const reset = () => {
    localRole.value = {};
    submitted.value = false;
    saving.value = false;
  };

  return {
    visible,
    localRole,
    submitted,
    saving,
    mode,
    isNewRole,
    open,
    close,
    reset,
  };
}
