import { computed, reactive } from 'vue';

export interface PasswordValidationState {
  currentPassword?: string;
  newPassword: string;
  confirmPassword: string;
}

export interface PasswordSecurityChecks {
  minLength: boolean;
  uppercase: boolean;
  lowercase: boolean;
  digit: boolean;
  specialChar: boolean;
}

export interface FieldTouched {
  currentPassword?: boolean;
  newPassword: boolean;
  confirmPassword: boolean;
}

export function usePasswordValidation(includeCurrent = false) {
  // ========================================
  // STATE
  // ========================================

  const passwords = reactive<PasswordValidationState>({
    currentPassword: includeCurrent ? '' : undefined,
    newPassword: '',
    confirmPassword: '',
  });

  const fieldTouched = reactive<FieldTouched>({
    currentPassword: includeCurrent ? false : undefined,
    newPassword: false,
    confirmPassword: false,
  });

  // ========================================
  // COMPUTED
  // ========================================

  const isPasswordSecure = computed<PasswordSecurityChecks>(() => {
    const password = passwords.newPassword;
    return {
      minLength: password.length >= 9,
      uppercase: /[A-Z]/.test(password),
      lowercase: /[a-z]/.test(password),
      digit: /\d/.test(password),
      specialChar: /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password),
    };
  });

  const isNewPasswordSecure = computed<boolean>(() => {
    const s = isPasswordSecure.value;
    return s.minLength && s.uppercase && s.lowercase && s.digit && s.specialChar;
  });

  const doPasswordsMatch = computed<boolean>(
    () => passwords.newPassword === passwords.confirmPassword && passwords.confirmPassword !== '',
  );

  // ========================================
  // ERROR METHODS
  // ========================================

  function getFieldError(key: keyof FieldTouched): boolean {
    if (!fieldTouched[key]) return false;

    if (key === 'currentPassword') {
      return includeCurrent && !passwords.currentPassword;
    }
    if (key === 'newPassword') {
      return !passwords.newPassword || !isNewPasswordSecure.value;
    }
    if (key === 'confirmPassword') {
      return !passwords.confirmPassword || !doPasswordsMatch.value;
    }
    return false;
  }

  function getFieldErrorMessage(key: keyof FieldTouched) {
    if (!fieldTouched[key]) return '';

    if (key === 'currentPassword') return 'Current password is required.';
    if (key === 'newPassword') {
      if (!passwords.newPassword) return 'New password is required.';
      if (!isNewPasswordSecure.value) return 'Password does not meet security requirements.';
    }
    if (key === 'confirmPassword') {
      if (!passwords.confirmPassword) return 'Please confirm the new password.';
      if (!doPasswordsMatch.value) return 'Passwords do not match.';
    }
    return '';
  }

  // ========================================
  // HELPERS
  // ========================================

  function markFieldTouched(key: keyof FieldTouched) {
    fieldTouched[key] = true;
  }

  function markAllFieldsTouched() {
    (Object.keys(fieldTouched) as Array<keyof FieldTouched>).forEach((key) => {
      fieldTouched[key] = true;
    });
  }

  function resetPasswords() {
    if (includeCurrent) passwords.currentPassword = '';
    passwords.newPassword = '';
    passwords.confirmPassword = '';

    if (includeCurrent) fieldTouched.currentPassword = false;
    fieldTouched.newPassword = false;
    fieldTouched.confirmPassword = false;
  }

  return {
    passwords,
    fieldTouched,

    isPasswordSecure,
    isNewPasswordSecure,
    doPasswordsMatch,

    getFieldError,
    getFieldErrorMessage,
    markFieldTouched,
    markAllFieldsTouched,
    resetPasswords,
  };
}
