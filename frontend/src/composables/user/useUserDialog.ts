// src/composables/user/useUserDialog.ts
import { ref, computed } from 'vue';
import type { UserResponseDto } from '@/types/backend';

export function useUserDialog() {
  const visible = ref(false);
  const localUser = ref<Partial<UserResponseDto>>({});
  const submitted = ref(false);
  const saving = ref(false);

  const mode = computed(() => (localUser.value.id ? 'edit' : 'create'));

  const isNewUser = computed(() => !localUser.value.id);

  const open = (user?: Partial<UserResponseDto>) => {
    if (user) {
      localUser.value = { ...user };
    } else {
      localUser.value = {};
    }
    submitted.value = false;
    visible.value = true;
  };

  const close = () => {
    visible.value = false;
    submitted.value = false;
    localUser.value = {};
  };

  const reset = () => {
    localUser.value = {};
    submitted.value = false;
    saving.value = false;
  };

  return {
    visible,
    localUser,
    submitted,
    saving,
    mode,
    isNewUser,
    open,
    close,
    reset,
  };
}