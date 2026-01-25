// src/composables/user/useUserForm.ts
import type { UserResponseDto } from '@/types/backend';
import { validateEmailFormat, validateUserFullName } from '@/utils/users/userValidation';
import { computed, type Ref } from 'vue';

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

  // NEW FIELDS
  const phoneNumber = computed({
    get: () => user.value.phoneNumber || '',
    set: (val) => {
      if (user.value) user.value.phoneNumber = val;
    },
  });

  const address = computed({
    get: () => user.value.address || '',
    set: (val) => {
      if (user.value) user.value.address = val;
    },
  });

  const jobTitle = computed({
    get: () => user.value.jobTitle || '',
    set: (val) => {
      if (user.value) user.value.jobTitle = val;
    },
  });

  const profilePictureUrl = computed({
    get: () => user.value.profilePictureUrl || '',
    set: (val) => {
      if (user.value) user.value.profilePictureUrl = val;
    },
  });

  const bio = computed({
    get: () => user.value.bio || '',
    set: (val) => {
      if (user.value) user.value.bio = val;
    },
  });

  const licenseNumber = computed({
    get: () => user.value.licenseNumber || '',
    set: (val) => {
      if (user.value) user.value.licenseNumber = val;
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
    // New exports
    phoneNumber,
    address,
    jobTitle,
    profilePictureUrl,
    bio,
    licenseNumber,
    // Validation
    isFullNameValid,
    isEmailValid,
    isRoleSelected,
    isClinicSelected,
    isFormValid,
  };
}
