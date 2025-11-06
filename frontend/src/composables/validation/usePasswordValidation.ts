import { computed, reactive } from "vue";

/**
 * Composable for shared password validation logic
 * Location: src/composables/usePasswordValidation.ts
 *
 * Purpose:
 * - Centralized password security validation
 * - Reusable across multiple components
 * - Consistent validation rules
 *
 * Features:
 * ✅ Individual requirement checking
 * ✅ Overall security validation
 * ✅ Field error detection
 * ✅ Touch tracking for fields
 * ✅ Error messages
 *
 * Author: Rabee Qabaha
 * Updated: 2025-11-04 20:06:34 UTC
 */

export interface PasswordValidationState {
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
  newPassword: boolean;
  confirmPassword: boolean;
}

/**
 * Composable hook for password validation
 * Provides reactive password state and validation helpers
 */
export function usePasswordValidation() {
  // ========================================
  // STATE
  // ========================================

  const passwords = reactive<PasswordValidationState>({
    newPassword: "",
    confirmPassword: "",
  });

  const fieldTouched = reactive<FieldTouched>({
    newPassword: false,
    confirmPassword: false,
  });

  // ========================================
  // COMPUTED - SECURITY CHECKS
  // ========================================

  /**
   * Check individual password security requirements
   */
  const isPasswordSecure = computed<PasswordSecurityChecks>(() => {
    const password = passwords.newPassword;
    return {
      minLength: password.length >= 12,
      uppercase: /[A-Z]/.test(password),
      lowercase: /[a-z]/.test(password),
      digit: /\d/.test(password),
      specialChar: /[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]/.test(password),
    };
  });

  /**
   * Check if password meets all security requirements
   */
  const isNewPasswordSecure = computed<boolean>(() => {
    const security = isPasswordSecure.value;
    return (
      security.minLength &&
      security.uppercase &&
      security.lowercase &&
      security.digit &&
      security.specialChar
    );
  });

  /**
   * Check if passwords match
   */
  const doPasswordsMatch = computed<boolean>(
    () =>
      passwords.newPassword === passwords.confirmPassword &&
      passwords.confirmPassword !== ""
  );

  // ========================================
  // HELPER FUNCTIONS
  // ========================================

  /**
   * Get validation error for a specific field
   * @param key - Field name (newPassword or confirmPassword)
   * @returns true if field has validation error
   */
  function getFieldError(key: keyof FieldTouched): boolean {
    if (!fieldTouched[key]) return false;

    if (key === "newPassword") {
      return !passwords.newPassword || !isNewPasswordSecure.value;
    }

    if (key === "confirmPassword") {
      return !passwords.confirmPassword || !doPasswordsMatch.value;
    }

    return false;
  }

  /**
   * Mark field as touched
   * @param key - Field name
   */
  function markFieldTouched(key: keyof FieldTouched): void {
    fieldTouched[key] = true;
  }

  /**
   * Reset password fields to initial state
   */
  function resetPasswords(): void {
    passwords.newPassword = "";
    passwords.confirmPassword = "";
    fieldTouched.newPassword = false;
    fieldTouched.confirmPassword = false;
  }

  /**
   * Get error message for a password field
   * @param key - Field name
   * @returns Error message string
   */
  function getFieldErrorMessage(key: keyof FieldTouched): string {
    if (!fieldTouched[key]) return "";

    if (key === "newPassword") {
      if (!passwords.newPassword) return "New password is required.";
      if (!isNewPasswordSecure.value)
        return "Password does not meet all security requirements.";
      return "";
    }

    if (key === "confirmPassword") {
      if (!passwords.confirmPassword)
        return "Password confirmation is required.";
      if (!doPasswordsMatch.value) return "Passwords do not match.";
      return "";
    }

    return "";
  }

  return {
    // State
    passwords,
    fieldTouched,

    // Computed - Security checks only
    isPasswordSecure,
    isNewPasswordSecure,
    doPasswordsMatch,

    // Methods - Password related only
    getFieldError,
    markFieldTouched,
    resetPasswords,
    getFieldErrorMessage,
  };
}
