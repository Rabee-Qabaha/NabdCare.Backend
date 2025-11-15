// src/composables/user/useUserForm.ts
import { computed, type Ref } from 'vue';
import type { UserResponseDto } from '@/types/backend';
import { validateUserFullName, validateEmailFormat } from '@/utils/users/userValidation';

export function useUserForm(user: Ref<Partial<UserResponseDto>>) {
  const fullName = computed({
    get: () => user.value.fullName || '',
    set: (val) => {
      if (user.value) user.value.fullName = val;
    },
  });

  const email = computed({
    get: () => user.value.email || '',
    set: (val) => {
      if (user.value) user.value.email = val;
    },
  });

  const roleId = computed({
    get: () => user.value.roleId || '',
    set: (val) => {
      if (user.value) user.value.roleId = val;
    },
  });

  const clinicId = computed({
    get: () => user.value.clinicId || '',
    set: (val) => {
      if (user.value) user.value.clinicId = val;
    },
  });

  const isActive = computed({
    get: () => user.value.isActive ?? true,
    set: (val) => {
      if (user.value) user.value.isActive = val;
    },
  });

  const isFullNameValid = computed(() => validateUserFullName(fullName.value));

  const isEmailValid = computed(() => validateEmailFormat(email.value));

  const isRoleSelected = computed(() => !!roleId.value);

  const isClinicSelected = computed(() => !!clinicId.value);

  const isFormValid = computed(
    () => isFullNameValid.value && isEmailValid.value && isRoleSelected.value,
  );

  return {
    fullName,
    email,
    roleId,
    clinicId,
    isActive,
    isFullNameValid,
    isEmailValid,
    isRoleSelected,
    isClinicSelected,
    isFormValid,
  };
}